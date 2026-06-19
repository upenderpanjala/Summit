using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Authorization;
using Summit.VMS.Data;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;
using Summit.VMS.ViewModels;

namespace Summit.VMS.Controllers;

[Authorize(Policy = Policies.ViewCases)]
public class CasesController : Controller
{
    private readonly ICaseService _cases;
    private readonly ApplicationDbContext _db;

    public CasesController(ICaseService cases, ApplicationDbContext db)
    {
        _cases = cases;
        _db = db;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        return View(await _cases.GetAllAsync(search));
    }

    public async Task<IActionResult> Details(int id)
    {
        var c = await _cases.GetByIdAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpGet, Authorize(Policy = Policies.ManageCases)]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new CaseFormViewModel());
    }

    [HttpPost, Authorize(Policy = Policies.ManageCases), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CaseFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(vm);
            return View(vm);
        }
        await _cases.CreateAsync(new CaseRecord
        {
            CaseNumber = vm.CaseNumber, Title = vm.Title, Description = vm.Description,
            Type = vm.Type, Status = vm.Status, Location = vm.Location,
            PoliceStationId = vm.PoliceStationId, AssignedOfficerId = vm.AssignedOfficerId,
            DateReportedUtc = DateTime.UtcNow
        });
        TempData["Toast"] = "Case created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Policies.ManageCases)]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _cases.GetByIdAsync(id);
        if (c == null) return NotFound();
        await PopulateLookupsAsync();
        return View(new CaseFormViewModel
        {
            Id = c.Id, CaseNumber = c.CaseNumber, Title = c.Title,
            Description = c.Description, Type = c.Type, Status = c.Status,
            Location = c.Location, PoliceStationId = c.PoliceStationId,
            AssignedOfficerId = c.AssignedOfficerId
        });
    }

    [HttpPost, Authorize(Policy = Policies.ManageCases), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CaseFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(vm);
            return View(vm);
        }
        var existing = await _cases.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.CaseNumber = vm.CaseNumber; existing.Title = vm.Title;
        existing.Description = vm.Description; existing.Type = vm.Type;
        existing.Status = vm.Status; existing.Location = vm.Location;
        existing.PoliceStationId = vm.PoliceStationId;
        existing.AssignedOfficerId = vm.AssignedOfficerId;

        await _cases.UpdateAsync(existing);
        TempData["Toast"] = "Case updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize(Policy = Policies.ManageCases), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _cases.DeleteAsync(id);
        TempData["Toast"] = "Case deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(CaseFormViewModel? vm = null)
    {
        var stations = await _db.PoliceStations.AsNoTracking().ToListAsync();
        ViewBag.Stations = new SelectList(stations, "Id", "Name", vm?.PoliceStationId);

        var officers = await _db.Users.AsNoTracking()
            .OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Officers = new SelectList(
            officers.Select(o => new { o.Id, Label = $"{o.FullName} ({o.Email})" }),
            "Id", "Label", vm?.AssignedOfficerId);
    }
}

[Authorize(Policy = Policies.ManageUsers)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;

    public UsersController(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var users = await _users.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var list = new List<UserListItemViewModel>();
        foreach (var u in users)
        {
            list.Add(new UserListItemViewModel
            {
                Id = u.Id, Email = u.Email ?? "", FullName = u.FullName,
                Mobile = u.Mobile, BadgeNumber = u.BadgeNumber, IsActive = u.IsActive,
                Roles = await _users.GetRolesAsync(u)
            });
        }
        return View(list);
    }
}
