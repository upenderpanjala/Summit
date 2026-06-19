using Microsoft.EntityFrameworkCore;
using Summit.VMS.Data;
using Summit.VMS.DTOs;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Services.Implementations;

public class VictimService : IVictimService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public VictimService(ApplicationDbContext db, IAuditService audit, INotificationService notifications)
    {
        _db = db;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<Victim>> GetAllAsync(string? search = null)
    {
        var q = _db.Victims.Include(v => v.Case).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(v =>
                v.ReferenceNumber.Contains(s) ||
                v.FirstName.Contains(s) ||
                v.LastName.Contains(s) ||
                (v.City != null && v.City.Contains(s)));
        }

        return await q.OrderByDescending(v => v.CreatedAtUtc).ToListAsync();
    }

    public async Task<Victim?> GetByIdAsync(int id, bool audit = false)
    {
        var victim = await _db.Victims
            .Include(v => v.Case)
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (victim != null && audit)
            await _audit.LogAsync("ViewVictim", nameof(Victim), id.ToString(),
                $"Viewed victim {victim.ReferenceNumber}");

        return victim;
    }

    public async Task<Victim> CreateAsync(VictimCreateUpdateDto dto)
    {
        var v = new Victim
        {
            ReferenceNumber = dto.ReferenceNumber.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            NationalId = dto.NationalId,
            ContactNumber = dto.ContactNumber,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Notes = dto.Notes,
            CaseId = dto.CaseId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Victims.Add(v);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("CreateVictim", nameof(Victim), v.Id.ToString(), v.ReferenceNumber);

        // Notify the oversight chain (global notification + email to all logins).
        await _notifications.NotifyVictimCreatedAsync(v);
        return v;
    }

    public async Task<bool> UpdateAsync(int id, VictimCreateUpdateDto dto)
    {
        var v = await _db.Victims.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return false;

        v.ReferenceNumber = dto.ReferenceNumber.Trim();
        v.FirstName = dto.FirstName.Trim();
        v.LastName = dto.LastName.Trim();
        v.Gender = dto.Gender;
        v.DateOfBirth = dto.DateOfBirth;
        v.NationalId = dto.NationalId;
        v.ContactNumber = dto.ContactNumber;
        v.Email = dto.Email;
        v.Address = dto.Address;
        v.City = dto.City;
        v.State = dto.State;
        v.Notes = dto.Notes;
        v.CaseId = dto.CaseId;
        v.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("UpdateVictim", nameof(Victim), id.ToString(), v.ReferenceNumber);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var v = await _db.Victims.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return false;
        _db.Victims.Remove(v);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("DeleteVictim", nameof(Victim), id.ToString(), v.ReferenceNumber);
        return true;
    }

    public Task<bool> ReferenceExistsAsync(string referenceNumber, int? excludeId = null)
    {
        var r = referenceNumber.Trim();
        return _db.Victims.AnyAsync(v =>
            v.ReferenceNumber == r && (excludeId == null || v.Id != excludeId));
    }
}

public class CaseService : ICaseService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CaseService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CaseRecord>> GetAllAsync(string? search = null)
    {
        var q = _db.Cases
            .Include(c => c.Victims)
            .Include(c => c.AssignedOfficer)
            .Include(c => c.PoliceStation)
            .AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.CaseNumber.Contains(s) || c.Title.Contains(s));
        }
        return await q.OrderByDescending(c => c.DateReportedUtc).ToListAsync();
    }

    public Task<CaseRecord?> GetByIdAsync(int id) =>
        _db.Cases
            .Include(c => c.Victims)
            .Include(c => c.AssignedOfficer)
            .Include(c => c.PoliceStation)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CaseRecord> CreateAsync(CaseRecord entity)
    {
        entity.CreatedAtUtc = DateTime.UtcNow;
        _db.Cases.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("CreateCase", nameof(CaseRecord), entity.Id.ToString(), entity.CaseNumber);
        return entity;
    }

    public async Task<bool> UpdateAsync(CaseRecord entity)
    {
        var exists = await _db.Cases.AnyAsync(c => c.Id == entity.Id);
        if (!exists) return false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Cases.Update(entity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("UpdateCase", nameof(CaseRecord), entity.Id.ToString(), entity.CaseNumber);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var c = await _db.Cases.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return false;
        _db.Cases.Remove(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("DeleteCase", nameof(CaseRecord), id.ToString(), c.CaseNumber);
        return true;
    }
}
