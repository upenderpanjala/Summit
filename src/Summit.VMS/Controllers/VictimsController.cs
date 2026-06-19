using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Authorization;
using Summit.VMS.Data;
using Summit.VMS.DTOs;
using Summit.VMS.Services.Interfaces;
using Summit.VMS.ViewModels;

namespace Summit.VMS.Controllers;

/// <summary>
/// MVC controller for victims.
///
/// Access model (the core requirement):
///   * Index/Details  -> ViewVictims policy
///         (Administrator, Investigator, PoliceHierarchy, HomeMinister)
///   * Create/Edit/Delete -> ManageVictims policy
///         (Administrator, Investigator ONLY)
///
/// The police hierarchy and the Home Minister can therefore log in and only
/// view victim details; every mutating action is blocked for them.
/// </summary>
[Authorize(Policy = Policies.ViewVictims)]
public class VictimsController : Controller
{
    private readonly IVictimService _victims;
    private readonly ApplicationDbContext _db;

    public VictimsController(IVictimService victims, ApplicationDbContext db)
    {
        _victims = victims;
        _db = db;
    }

    // GET: /Victims
    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        var list = await _victims.GetAllAsync(search);
        return View(list);
    }

    // GET: /Victims/Details/5  (auditing each view)
    public async Task<IActionResult> Details(int id)
    {
        var victim = await _victims.GetByIdAsync(id, audit: true);
        if (victim == null) return NotFound();
        return View(victim);
    }

    // ----- Mutating actions: Administrator + Investigator only -----

    [HttpGet, Authorize(Policy = Policies.ManageVictims)]
    public async Task<IActionResult> Create()
    {
        await PopulateCasesAsync();
        return View(new VictimFormViewModel());
    }

    [HttpPost, Authorize(Policy = Policies.ManageVictims), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VictimFormViewModel vm)
    {
        if (await _victims.ReferenceExistsAsync(vm.ReferenceNumber))
            ModelState.AddModelError(nameof(vm.ReferenceNumber), "Reference number already exists.");

        if (!ModelState.IsValid)
        {
            await PopulateCasesAsync(vm.CaseId);
            return View(vm);
        }

        await _victims.CreateAsync(ToDto(vm));
        TempData["Toast"] = "Victim record created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = Policies.ManageVictims)]
    public async Task<IActionResult> Edit(int id)
    {
        var v = await _victims.GetByIdAsync(id);
        if (v == null) return NotFound();
        await PopulateCasesAsync(v.CaseId);
        return View(new VictimFormViewModel
        {
            Id = v.Id, ReferenceNumber = v.ReferenceNumber,
            FirstName = v.FirstName, LastName = v.LastName, Gender = v.Gender,
            DateOfBirth = v.DateOfBirth, NationalId = v.NationalId,
            ContactNumber = v.ContactNumber, Email = v.Email, Address = v.Address,
            City = v.City, State = v.State, Notes = v.Notes, CaseId = v.CaseId
        });
    }

    [HttpPost, Authorize(Policy = Policies.ManageVictims), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VictimFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (await _victims.ReferenceExistsAsync(vm.ReferenceNumber, id))
            ModelState.AddModelError(nameof(vm.ReferenceNumber), "Reference number already exists.");

        if (!ModelState.IsValid)
        {
            await PopulateCasesAsync(vm.CaseId);
            return View(vm);
        }

        var ok = await _victims.UpdateAsync(id, ToDto(vm));
        if (!ok) return NotFound();
        TempData["Toast"] = "Victim record updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet, Authorize(Policy = Policies.ManageVictims)]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await _victims.GetByIdAsync(id);
        if (v == null) return NotFound();
        return View(v);
    }

    [HttpPost, Authorize(Policy = Policies.ManageVictims), ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _victims.DeleteAsync(id);
        TempData["Toast"] = "Victim record deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static VictimCreateUpdateDto ToDto(VictimFormViewModel vm) => new()
    {
        ReferenceNumber = vm.ReferenceNumber, FirstName = vm.FirstName,
        LastName = vm.LastName, Gender = vm.Gender, DateOfBirth = vm.DateOfBirth,
        NationalId = vm.NationalId, ContactNumber = vm.ContactNumber, Email = vm.Email,
        Address = vm.Address, City = vm.City, State = vm.State, Notes = vm.Notes,
        CaseId = vm.CaseId
    };

    private async Task PopulateCasesAsync(int? selected = null)
    {
        var cases = await _db.Cases.AsNoTracking()
            .OrderByDescending(c => c.DateReportedUtc).ToListAsync();
        ViewBag.Cases = new SelectList(
            cases.Select(c => new { c.Id, Label = $"{c.CaseNumber} — {c.Title}" }),
            "Id", "Label", selected);
    }
}
