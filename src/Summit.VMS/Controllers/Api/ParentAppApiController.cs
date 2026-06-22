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
/// Backend for the PARENT / concerned-person app. A parent registers only AFTER
/// the victim has registered, and only with a number the victim nominated. They
/// can view her live location (if she opted in), raise a request, and see status.
/// Anonymous for the prototype; production requires OTP + JWT (see DESIGN.md).
/// </summary>
[ApiController]
[Route("api/app/parent")]
[AllowAnonymous]
[Produces("application/json")]
public class ParentAppApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentWorkflowService _workflow;

    public ParentAppApiController(ApplicationDbContext db, IIncidentWorkflowService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    // 1) Register against the victim's mobile. Approved only if the parent's phone
    //    matches the victim's guardian number or one of her emergency contacts.
    [HttpPost("register")]
    public async Task<ActionResult<ParentLinkDto>> Register([FromBody] ParentRegisterDto dto)
    {
        var victimMobile = PhoneNumber.Normalize(dto.VictimMobile);
        var parentPhone = PhoneNumber.Normalize(dto.Phone);

        var victim = await _db.Set<VictimProfile>()
            .Include(v => v.Contacts)
            .FirstOrDefaultAsync(v => v.Mobile == victimMobile);
        if (victim == null)
            return NotFound(new { message = "No registered victim found for that mobile number." });

        var nominated = (victim.GuardianPhone == parentPhone)
            || victim.Contacts.Any(c => c.Phone == parentPhone);

        var reg = new GuardianAppRegistration
        {
            VictimProfileId = victim.Id, Name = dto.Name, Relation = dto.Relation,
            Phone = parentPhone, IsApproved = nominated,
            VerificationStatus = nominated ? VerificationStatus.Verified : VerificationStatus.Pending
        };
        _db.Add(reg);
        await _db.SaveChangesAsync();

        return Ok(new ParentLinkDto(victim.Id, victim.FullName, nominated,
            nominated
                ? "Linked. You can view location and raise a request."
                : "Submitted. Your number isn't on the victim's nominated list yet — pending approval."));
    }

    // 2) Live location + current concern/incident state
    [HttpGet("{victimId:int}/location")]
    public async Task<ActionResult<LiveLocationDto>> Location(int victimId)
    {
        var victim = await _db.Set<VictimProfile>()
            .Include(v => v.Contacts).Include(v => v.Incidents)
            .FirstOrDefaultAsync(v => v.Id == victimId);
        if (victim == null) return NotFound();

        var last = await _db.Set<LocationPing>()
            .Where(p => p.VictimProfileId == victimId)
            .OrderByDescending(p => p.CapturedAtUtc).FirstOrDefaultAsync();

        var concernCount = await _db.Set<ConcernRequest>()
            .Where(c => c.VictimProfileId == victimId)
            .Select(c => c.RaisedByPhone).Distinct().CountAsync();

        var incident = victim.Incidents.OrderByDescending(i => i.RaisedAtUtc).FirstOrDefault();

        return Ok(new LiveLocationDto(
            last?.Latitude, last?.Longitude, last?.OnRoute ?? true, last?.CapturedAtUtc,
            incident?.Status, concernCount, Math.Max(2, victim.Contacts.Count)));
    }

    // 3) Raise a request (e.g. off-route or unreachable). When enough distinct
    //    concerned people raise one, it escalates to the police automatically.
    [HttpPost("{victimId:int}/concern")]
    public async Task<IActionResult> Concern(int victimId, [FromBody] ConcernDto dto)
    {
        var (count, threshold, incident) =
            await _workflow.RaiseConcernAsync(victimId, PhoneNumber.Normalize(dto.Phone), dto.Name, dto.Reason);

        return Ok(new
        {
            concernCount = count,
            threshold,
            escalatedToPolice = incident != null,
            incidentId = incident?.Id,
            incidentStatus = incident?.Status
        });
    }
}
