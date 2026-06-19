using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Summit.VMS.Authorization;
using Summit.VMS.DTOs;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Controllers.Api;

/// <summary>
/// REST API for victims. Authenticated with JWT bearer tokens.
///   GET endpoints  -> ViewVictims  (all four roles)
///   write endpoints -> ManageVictims (Administrator, Investigator)
/// View-only roles (PoliceHierarchy, HomeMinister) receive 403 on writes.
/// </summary>
[ApiController]
[Route("api/victims")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class VictimsApiController : ControllerBase
{
    private readonly IVictimService _victims;

    public VictimsApiController(IVictimService victims) => _victims = victims;

    [HttpGet]
    [Authorize(Policy = Policies.ViewVictims)]
    public async Task<ActionResult<IEnumerable<VictimSummaryDto>>> GetAll([FromQuery] string? search)
    {
        var list = await _victims.GetAllAsync(search);
        return Ok(list.Select(v => new VictimSummaryDto(
            v.Id, v.ReferenceNumber, v.FullName, v.Gender, v.City,
            v.CaseId, v.Case?.CaseNumber)));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.ViewVictims)]
    public async Task<ActionResult<VictimDto>> Get(int id)
    {
        var v = await _victims.GetByIdAsync(id, audit: true);
        if (v == null) return NotFound();
        return Ok(new VictimDto(
            v.Id, v.ReferenceNumber, v.FirstName, v.LastName, v.Gender,
            v.DateOfBirth, v.NationalId, v.ContactNumber, v.Email, v.Address,
            v.City, v.State, v.Notes, v.CaseId, v.Case?.CaseNumber));
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageVictims)]
    public async Task<ActionResult<VictimDto>> Create([FromBody] VictimCreateUpdateDto dto)
    {
        if (await _victims.ReferenceExistsAsync(dto.ReferenceNumber))
            return Conflict(new { message = "Reference number already exists." });

        var v = await _victims.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, new VictimDto(
            v.Id, v.ReferenceNumber, v.FirstName, v.LastName, v.Gender,
            v.DateOfBirth, v.NationalId, v.ContactNumber, v.Email, v.Address,
            v.City, v.State, v.Notes, v.CaseId, null));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.ManageVictims)]
    public async Task<IActionResult> Update(int id, [FromBody] VictimCreateUpdateDto dto)
    {
        if (await _victims.ReferenceExistsAsync(dto.ReferenceNumber, id))
            return Conflict(new { message = "Reference number already exists." });

        var ok = await _victims.UpdateAsync(id, dto);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.ManageVictims)]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _victims.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

[ApiController]
[Route("api/cases")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class CasesApiController : ControllerBase
{
    private readonly ICaseService _cases;

    public CasesApiController(ICaseService cases) => _cases = cases;

    [HttpGet]
    [Authorize(Policy = Policies.ViewCases)]
    public async Task<ActionResult<IEnumerable<CaseSummaryDto>>> GetAll([FromQuery] string? search)
    {
        var list = await _cases.GetAllAsync(search);
        return Ok(list.Select(c => new CaseSummaryDto(
            c.Id, c.CaseNumber, c.Title, c.Type, c.Status,
            c.DateReportedUtc, c.Victims.Count)));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.ViewCases)]
    public async Task<ActionResult<CaseSummaryDto>> Get(int id)
    {
        var c = await _cases.GetByIdAsync(id);
        if (c == null) return NotFound();
        return Ok(new CaseSummaryDto(
            c.Id, c.CaseNumber, c.Title, c.Type, c.Status,
            c.DateReportedUtc, c.Victims.Count));
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageCases)]
    public async Task<ActionResult<CaseSummaryDto>> Create([FromBody] CaseRecord input)
    {
        var c = await _cases.CreateAsync(input);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new CaseSummaryDto(
            c.Id, c.CaseNumber, c.Title, c.Type, c.Status, c.DateReportedUtc, 0));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.ManageCases)]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _cases.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
