using Summit.VMS.DTOs;
using Summit.VMS.Models.Entities;

namespace Summit.VMS.Services.Interfaces;

public interface ITokenService
{
    AuthResponse CreateToken(ApplicationUser user, IList<string> roles);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId = null, string? details = null);
}

public interface IEmailSender
{
    /// <summary>Sends one message to many recipients. Never throws — failures are logged.</summary>
    Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody);
}

public interface INotificationService
{
    /// <summary>Records a global notification and emails the oversight recipients.</summary>
    Task NotifyVictimCreatedAsync(Victim victim);

    Task<IReadOnlyList<Notification>> GetRecentAsync(int take = 20);

    /// <summary>Count of notifications created in the last <paramref name="days"/> days.</summary>
    Task<int> GetRecentCountAsync(int days = 7);
}

public interface IDocumentService
{
    Task<VictimDocument> SaveAsync(int victimId, string fileName, string? contentType, Stream content);
    Task<IReadOnlyList<VictimDocument>> ListAsync(int victimId);
    Task<(VictimDocument meta, Stream content)?> OpenAsync(int documentId);
    Task<bool> DeleteAsync(int documentId);
}

public interface IVictimService
{
    Task<IReadOnlyList<Victim>> GetAllAsync(string? search = null);
    Task<Victim?> GetByIdAsync(int id, bool audit = false);
    Task<Victim> CreateAsync(VictimCreateUpdateDto dto);
    Task<bool> UpdateAsync(int id, VictimCreateUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReferenceExistsAsync(string referenceNumber, int? excludeId = null);
}

public interface ICaseService
{
    Task<IReadOnlyList<CaseRecord>> GetAllAsync(string? search = null);
    Task<CaseRecord?> GetByIdAsync(int id);
    Task<CaseRecord> CreateAsync(CaseRecord entity);
    Task<bool> UpdateAsync(CaseRecord entity);
    Task<bool> DeleteAsync(int id);
}
