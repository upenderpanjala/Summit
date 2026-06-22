using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Authorization;
using Summit.VMS.Data;
using Summit.VMS.DTOs;
using Summit.VMS.Models.Enums;
using Summit.VMS.Models.Safety;
using Summit.VMS.Services.Safety;

namespace Summit.VMS.Controllers.Api;

/// <summary>
/// Backend for the POLICE mobile app.
///
/// Staged visibility (a core requirement):
///   * "pending" returns incidents still under the initial check — only
///     station/handling officers act on these.
///   * "confirmed" returns verified+ incidents — what higher authorities see.
///     An unverified incident is never exposed to the hierarchy.
/// </summary>
[ApiController]
[Route("api/app/police")]
[Produces("application/json")]
public class PoliceAppApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentWorkflowService _workflow;

    public PoliceAppApiController(ApplicationDbContext db, IIncidentWorkflowService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    // 1) Officer self-registration (requires approval before going active)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] PoliceRegisterDto dto)
    {
        if (await _db.Set<PoliceAppRegistration>().AnyAsync(p => p.PoliceId == dto.PoliceId))
            return Conflict(new { message = "This police ID is already registered." });

        var reg = new PoliceAppRegistration
        {
            PoliceId = dto.PoliceId, FullName = dto.FullName, Rank = dto.Rank,
            PoliceStationId = dto.PoliceStationId, StationName = dto.StationName,
            Mobile = dto.Mobile, Email = dto.Email, IsApproved = false
        };
        _db.Add(reg);
        await _db.SaveChangesAsync();
        return Ok(new { reg.Id, reg.IsApproved, message = "Registered. Awaiting approval." });
    }

    // 2) Initial-check queue — station/handling officers only
    [HttpGet("pending")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Policy = Policies.ManageCases)]
    public async Task<ActionResult<IEnumerable<PendingIncidentDto>>> Pending()
    {
        var list = await _workflow.GetPendingForOfficerAsync();
        return Ok(list.Select(i => new PendingIncidentDto(
            i.Id, i.VictimProfile?.FullName ?? "", i.VictimProfile?.Mobile ?? "", i.Status,
            i.VictimProfile?.GuardianName, i.VictimProfile?.GuardianPhone,
            i.VictimProfile?.District, i.VictimProfile?.State?.ToString(),
            i.Latitude, i.Longitude, i.RaisedAtUtc,
            i.Verifications.Select(v =>
            {
                var c = i.VictimProfile?.Contacts.FirstOrDefault(x => x.Id == v.EmergencyContactId);
                return new ContactCheckDto(v.EmergencyContactId, c?.Name ?? "",
                    c?.Relation ?? ContactRelation.Other, c?.Phone ?? "", v.Decision);
            }))));
    }

    // 3) Record a contact's response to the initial check
    [HttpPost("incident/{incidentId:int}/verify")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Policy = Policies.ManageCases)]
    public async Task<ActionResult<IncidentStateDto>> Verify(
        int incidentId, [FromBody] VerificationDecisionDto dto)
    {
        var officerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var incident = await _workflow.RecordVerificationAsync(
            incidentId, dto.ContactId, dto.Decision, officerId);
        if (incident == null) return NotFound();

        return Ok(new IncidentStateDto(
            incident.Id, incident.Status, incident.VoiceMatchScore,
            incident.Latitude, incident.Longitude, incident.RaisedAtUtc, incident.CaseId));
    }

    // 4) Confirmed incidents with full info + last location — higher authorities
    [HttpGet("confirmed")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Policy = Policies.ViewCases)]
    public async Task<ActionResult<IEnumerable<ConfirmedIncidentDto>>> Confirmed()
    {
        var list = await _workflow.GetForHierarchyAsync();
        return Ok(list.Select(i => new ConfirmedIncidentDto(
            i.Id, i.VictimProfile?.FullName ?? "", i.VictimProfile?.Mobile ?? "", i.Status,
            i.Latitude, i.Longitude, i.ConfirmedAtUtc, i.CaseId)));
    }

    // 5) IVR webhook — telephony provider posts the contact's keypad/voice response.
    //    Secured in production by provider signature validation (see DESIGN.md).
    [HttpPost("/api/app/ivr/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> IvrCallback(
        [FromForm] int incidentId, [FromForm] int contactId, [FromForm] string response)
    {
        var decision = response?.Trim().ToLowerInvariant() switch
        {
            "1" or "yes" or "confirm" => VerificationDecision.ConfirmedMissing,
            "2" or "no" or "mistake" => VerificationDecision.DeniedFalseAlarm,
            _ => VerificationDecision.NoResponse
        };
        var incident = await _workflow.RecordVerificationAsync(incidentId, contactId, decision);
        return incident == null ? NotFound() : Ok(new { incident.Status });
    }
}
