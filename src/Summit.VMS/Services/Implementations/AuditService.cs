using System.Security.Claims;
using Summit.VMS.Data;
using Summit.VMS.Models.Entities;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string entityType, string? entityId = null, string? details = null)
    {
        var user = _http.HttpContext?.User;
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = user?.Identity?.Name,
            IpAddress = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            TimestampUtc = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
