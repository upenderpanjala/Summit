using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Data;
using Summit.VMS.Models.Entities;
using Summit.VMS.Models.Enums;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _http;

    public NotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IEmailSender email,
        IConfiguration config,
        IHttpContextAccessor http)
    {
        _db = db;
        _users = users;
        _email = email;
        _config = config;
        _http = http;
    }

    public async Task NotifyVictimCreatedAsync(Victim victim)
    {
        var actor = _http.HttpContext?.User?.Identity?.Name ?? "system";

        // 1) Persist a global notification shown to every signed-in user.
        var note = new Notification
        {
            Title = $"New victim record: {victim.ReferenceNumber}",
            Message = $"{victim.FullName} was added by {actor}.",
            EntityType = nameof(Victim),
            EntityId = victim.Id.ToString(),
            CreatedByName = actor,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Notifications.Add(note);
        await _db.SaveChangesAsync();

        // 2) Email the oversight chain (everyone with victim-view access).
        var recipients = await GetOversightEmailsAsync();
        var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/') ?? "";
        var link = $"{baseUrl}/Victims/Details/{victim.Id}";

        var html = $@"
<div style='font-family:Arial,sans-serif'>
  <h2 style='color:#004B8E;margin-bottom:4px'>New victim record created</h2>
  <p style='color:#F68026;font-weight:bold;margin-top:0'>Reference {victim.ReferenceNumber}</p>
  <table cellpadding='4' style='border-collapse:collapse'>
    <tr><td><b>Name</b></td><td>{victim.FullName}</td></tr>
    <tr><td><b>Gender</b></td><td>{victim.Gender}</td></tr>
    <tr><td><b>Mobile</b></td><td>{victim.ContactNumber ?? "—"}</td></tr>
    <tr><td><b>City</b></td><td>{victim.City ?? "—"}</td></tr>
    <tr><td><b>Added by</b></td><td>{actor}</td></tr>
  </table>
  <p style='margin-top:16px'>
    <a href='{link}' style='background:#004B8E;color:#fff;padding:8px 14px;
       text-decoration:none;border-radius:4px'>Open record</a>
  </p>
  <p style='color:#888;font-size:12px'>Summit VMS — automated notification.</p>
</div>";

        await _email.SendAsync(recipients, $"[Summit VMS] New victim {victim.ReferenceNumber}", html);
    }

    public async Task NotifyAsync(string title, string message, string entityType,
        string? entityId, string? linkPath = null, bool email = true)
    {
        var actor = _http.HttpContext?.User?.Identity?.Name ?? "system";
        _db.Notifications.Add(new Notification
        {
            Title = title, Message = message, EntityType = entityType,
            EntityId = entityId, CreatedByName = actor, CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (!email) return;
        var recipients = await GetOversightEmailsAsync();
        var baseUrl = _config["App:BaseUrl"]?.TrimEnd('/') ?? "";
        var link = string.IsNullOrEmpty(linkPath) ? baseUrl : $"{baseUrl}{linkPath}";
        var html = $@"
<div style='font-family:Arial,sans-serif'>
  <h2 style='color:#004B8E;margin-bottom:4px'>{title}</h2>
  <p style='margin-top:0'>{message}</p>
  <p style='margin-top:16px'>
    <a href='{link}' style='background:#F68026;color:#fff;padding:8px 14px;
       text-decoration:none;border-radius:4px'>Open in Summit VMS</a>
  </p>
  <p style='color:#888;font-size:12px'>Summit VMS — automated notification.</p>
</div>";
        await _email.SendAsync(recipients, $"[Summit VMS] {title}", html);
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(int take = 20) =>
        await _db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync();

    public Task<int> GetRecentCountAsync(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return _db.Notifications.CountAsync(n => n.CreatedAtUtc >= since);
    }

    private async Task<List<string>> GetOversightEmailsAsync()
    {
        var emails = new List<string>();
        foreach (var role in new[]
                 {
                     AppRoles.Administrator, AppRoles.Investigator,
                     AppRoles.PoliceHierarchy, AppRoles.HomeMinister
                 })
        {
            var inRole = await _users.GetUsersInRoleAsync(role);
            emails.AddRange(inRole.Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                                  .Select(u => u.Email!));
        }
        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _http;
    private readonly IAuditService _audit;

    public DocumentService(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        IConfiguration config,
        IHttpContextAccessor http,
        IAuditService audit)
    {
        _db = db;
        _env = env;
        _config = config;
        _http = http;
        _audit = audit;
    }

    private string Root
    {
        get
        {
            var configured = _config["Storage:DocumentsPath"] ?? "Storage/documents";
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(_env.ContentRootPath, configured);
        }
    }

    public async Task<VictimDocument> SaveAsync(int victimId, string fileName, string? contentType, Stream content)
    {
        var safeName = Path.GetFileName(fileName);
        var victimDir = Path.Combine(Root, victimId.ToString());
        Directory.CreateDirectory(victimDir);

        var unique = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(victimDir, unique);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
            await content.CopyToAsync(fs);

        var user = _http.HttpContext?.User;
        var doc = new VictimDocument
        {
            VictimId = victimId,
            FileName = safeName,
            ContentType = contentType,
            SizeBytes = new FileInfo(fullPath).Length,
            StoredPath = Path.Combine(victimId.ToString(), unique),
            UploadedById = user?.FindFirstValue(ClaimTypes.NameIdentifier),
            UploadedByName = user?.Identity?.Name,
            UploadedAtUtc = DateTime.UtcNow
        };
        _db.VictimDocuments.Add(doc);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("UploadDocument", nameof(VictimDocument), doc.Id.ToString(), safeName);
        return doc;
    }

    public async Task<IReadOnlyList<VictimDocument>> ListAsync(int victimId) =>
        await _db.VictimDocuments.AsNoTracking()
            .Where(d => d.VictimId == victimId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();

    public async Task<(VictimDocument meta, Stream content)?> OpenAsync(int documentId)
    {
        var doc = await _db.VictimDocuments.FindAsync(documentId);
        if (doc == null) return null;

        var fullPath = Path.Combine(Root, doc.StoredPath);
        if (!File.Exists(fullPath)) return null;

        await _audit.LogAsync("DownloadDocument", nameof(VictimDocument), doc.Id.ToString(), doc.FileName);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (doc, stream);
    }

    public async Task<bool> DeleteAsync(int documentId)
    {
        var doc = await _db.VictimDocuments.FindAsync(documentId);
        if (doc == null) return false;

        var fullPath = Path.Combine(Root, doc.StoredPath);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        _db.VictimDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("DeleteDocument", nameof(VictimDocument), documentId.ToString(), doc.FileName);
        return true;
    }
}
