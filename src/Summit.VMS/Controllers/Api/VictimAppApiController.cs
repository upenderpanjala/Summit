using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Data;
using Summit.VMS.DTOs;
using Summit.VMS.Models.Safety;
using Summit.VMS.Services.Safety;
using Summit.VMS.Validation;

namespace Summit.VMS.Controllers.Api;

/// <summary>
/// Backend for the VICTIM mobile app. Onboarding endpoints are anonymous for the
/// prototype; in production every call is authenticated (OTP/JWT) — see DESIGN.md.
/// </summary>
[ApiController]
[Route("api/app/victim")]
[AllowAnonymous]
[Produces("application/json")]
public class VictimAppApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISummitVerifier _verifier;
    private readonly IOtpSender _otp;
    private readonly IVerificationCaller _caller;
    private readonly IVoiceMatchService _voice;
    private readonly IIncidentWorkflowService _workflow;
    private readonly IWebHostEnvironment _env;

    public VictimAppApiController(
        ApplicationDbContext db, ISummitVerifier verifier, IOtpSender otp, IVerificationCaller caller,
        IVoiceMatchService voice, IIncidentWorkflowService workflow, IWebHostEnvironment env)
    {
        _db = db; _verifier = verifier; _otp = otp; _caller = caller;
        _voice = voice; _workflow = workflow; _env = env;
    }

    // 1) Basic registration -> PendingVerification
    [HttpPost("register")]
    public async Task<ActionResult<VictimProfileStateDto>> Register([FromBody] VictimRegisterDto dto)
    {
        var profile = new VictimProfile
        {
            FullName = dto.FullName, Gender = dto.Gender, DateOfBirth = dto.DateOfBirth,
            Mobile = PhoneNumber.Normalize(dto.Mobile), Email = dto.Email, Address = dto.Address,
            City = dto.City, State = dto.State, District = dto.District,
            GuardianName = dto.GuardianName, GuardianPhone = PhoneNumber.Normalize(dto.GuardianPhone),
            RegistrationStatus = RegistrationStatus.PendingVerification
        };
        _db.Add(profile);
        await _db.SaveChangesAsync();
        return Ok(ToState(profile, 0, 0, null));
    }

    // 2a) Send an OTP to the parent/guardian phone
    [HttpPost("{id:int}/otp/send")]
    public async Task<IActionResult> SendOtp(int id)
    {
        var profile = await _db.Set<VictimProfile>().FindAsync(id);
        if (profile == null) return NotFound();
        if (string.IsNullOrWhiteSpace(profile.GuardianPhone))
            return BadRequest(new { message = "No parent/guardian phone on file." });

        // Resend cooldown — avoid OTP spamming.
        if (profile.OtpSentAtUtc is { } sent && (DateTime.UtcNow - sent).TotalSeconds < 30)
            return StatusCode(429, new { message = "Please wait before requesting another OTP." });

        var (code, devCode) = await _otp.SendAsync(profile.GuardianPhone);
        profile.OtpCode = code;
        profile.OtpExpiresUtc = DateTime.UtcNow.AddMinutes(5);
        profile.OtpSentAtUtc = DateTime.UtcNow;
        profile.OtpAttempts = 0;
        await _db.SaveChangesAsync();

        var masked = profile.GuardianPhone.Length >= 4
            ? new string('•', profile.GuardianPhone.Length - 4) + profile.GuardianPhone[^4..]
            : profile.GuardianPhone;
        return Ok(new { sentTo = masked, devCode }); // devCode is null with a real SMS provider
    }

    // 2b) Verify the OTP -> issues the Summit token and marks the profile verified
    [HttpPost("{id:int}/otp/verify")]
    public async Task<IActionResult> VerifyOtp(int id, [FromBody] OtpVerifyDto dto)
    {
        var profile = await _db.Set<VictimProfile>().FindAsync(id);
        if (profile == null) return NotFound();

        if (profile.OtpCode == null || profile.OtpExpiresUtc < DateTime.UtcNow)
            return BadRequest(new { message = "OTP expired — request a new one." });
        if (profile.OtpAttempts >= 5)
        {
            profile.OtpCode = null;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Too many attempts — request a new OTP." });
        }
        if (profile.OtpCode != dto.Code)
        {
            profile.OtpAttempts++;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = $"Incorrect OTP. {5 - profile.OtpAttempts} attempt(s) left." });
        }

        var outcome = await _verifier.VerifyAsync(profile);
        profile.VerificationStatus = VerificationStatus.Verified;
        profile.VerificationToken = outcome.Token;
        profile.VerifiedAtUtc = DateTime.UtcNow;
        profile.OtpCode = null; profile.OtpExpiresUtc = null; profile.OtpAttempts = 0;
        profile.RegistrationStatus = RegistrationStatus.PendingContactConsent;
        await _db.SaveChangesAsync();
        return Ok(new { profile.VerificationStatus, profile.RegistrationStatus });
    }

    // 3) Add emergency contacts (>=3) and place informational/consent calls
    [HttpPost("{id:int}/contacts")]
    public async Task<IActionResult> AddContacts(int id, [FromBody] List<EmergencyContactDto> contacts)
    {
        var profile = await _db.Set<VictimProfile>()
            .Include(p => p.Contacts).FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null) return NotFound();
        if (contacts.Count < 3)
            return BadRequest(new { message = "At least 3 contacts (family and/or friends) are required." });

        foreach (var c in contacts)
        {
            var contact = new EmergencyContact
            {
                VictimProfileId = profile.Id, Name = c.Name, Relation = c.Relation,
                Phone = PhoneNumber.Normalize(c.Phone)
            };
            var call = await _caller.PlaceConsentCallAsync(contact, profile);
            if (call.Placed) contact.InformedAtUtc = DateTime.UtcNow;
            _db.Add(contact);
        }
        profile.RegistrationStatus = RegistrationStatus.PendingVoiceEnrollment;
        await _db.SaveChangesAsync();
        return Ok(new { added = contacts.Count, profile.RegistrationStatus });
    }

    // 4) Enrol a voice sample (call 3x at registration)
    [HttpPost("{id:int}/voice-samples")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> EnrollVoice(int id, IFormFile audio, [FromForm] int ordinal)
    {
        var profile = await _db.Set<VictimProfile>()
            .Include(p => p.VoiceSamples).FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null) return NotFound();
        if (audio is null || audio.Length == 0) return BadRequest(new { message = "Audio required." });

        var path = await SaveAudioAsync(id, $"sample{ordinal}", audio);
        await using var stream = audio.OpenReadStream();
        var voiceprintId = await _voice.EnrollAsync(stream);

        _db.Add(new DistressVoiceSample
        {
            VictimProfileId = profile.Id, StoredPath = path,
            VoiceprintId = voiceprintId, Ordinal = ordinal
        });

        if (profile.VoiceSamples.Count + 1 >= 3)
            profile.RegistrationStatus = RegistrationStatus.Active;
        await _db.SaveChangesAsync();
        return Ok(new { enrolled = true, profile.RegistrationStatus });
    }

    // 5) SOS — victim sends the distress voice + location
    [HttpPost("{id:int}/sos")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<IncidentStateDto>> Sos(
        int id, IFormFile? audio, [FromForm] double? lat, [FromForm] double? lng)
    {
        Stream? stream = null;
        if (audio is { Length: > 0 })
        {
            await SaveAudioAsync(id, $"trigger-{DateTime.UtcNow:yyyyMMddHHmmss}", audio);
            stream = audio.OpenReadStream();
        }

        var incident = await _workflow.RaiseAsync(id, stream, lat, lng);
        if (stream != null) await stream.DisposeAsync();

        return Ok(new IncidentStateDto(
            incident.Id, incident.Status, incident.VoiceMatchScore,
            incident.Latitude, incident.Longitude, incident.RaisedAtUtc, incident.CaseId));
    }

    // 6) Cancel within the grace window (mistaken trigger)
    [HttpPost("/api/app/incident/{incidentId:int}/cancel")]
    public async Task<IActionResult> Cancel(int incidentId)
    {
        var ok = await _workflow.CancelAsync(incidentId);
        return ok ? Ok(new { cancelled = true })
                  : BadRequest(new { message = "Incident can no longer be cancelled." });
    }

    // 7) Share live location (periodic ping for approved parents to view)
    [HttpPost("{id:int}/location")]
    public async Task<IActionResult> ShareLocation(int id, [FromBody] LocationPingDto dto)
    {
        if (!await _db.Set<VictimProfile>().AnyAsync(v => v.Id == id)) return NotFound();
        _db.Add(new Models.Safety.LocationPing
        {
            VictimProfileId = id, Latitude = dto.Lat, Longitude = dto.Lng, OnRoute = dto.OnRoute
        });
        await _db.SaveChangesAsync();
        return Ok(new { shared = true });
    }

    // 8) Status polling
    [HttpGet("{id:int}/status")]
    public async Task<ActionResult<VictimProfileStateDto>> Status(int id)
    {
        var profile = await _db.Set<VictimProfile>()
            .Include(p => p.Contacts).Include(p => p.VoiceSamples).Include(p => p.Incidents)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (profile == null) return NotFound();

        var active = profile.Incidents
            .OrderByDescending(i => i.RaisedAtUtc).FirstOrDefault();
        return Ok(ToState(profile, profile.Contacts.Count, profile.VoiceSamples.Count, active?.Status));
    }

    private static VictimProfileStateDto ToState(VictimProfile p, int contacts, int samples, IncidentStatus? active)
        => new(p.Id, p.FullName, p.RegistrationStatus, p.VerificationStatus, contacts, samples, active);

    private async Task<string> SaveAudioAsync(int victimId, string name, IFormFile file)
    {
        var dir = Path.Combine(_env.ContentRootPath, "Storage", "voice", victimId.ToString());
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName);
        var rel = Path.Combine("voice", victimId.ToString(), $"{name}_{Guid.NewGuid():N}{ext}");
        var full = Path.Combine(_env.ContentRootPath, "Storage", rel);
        await using var fs = new FileStream(full, FileMode.Create);
        await file.CopyToAsync(fs);
        return rel;
    }
}
