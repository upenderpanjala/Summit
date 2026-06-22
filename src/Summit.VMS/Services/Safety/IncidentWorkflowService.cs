using Microsoft.EntityFrameworkCore;
using Summit.VMS.Data;
using Summit.VMS.Models.Entities;
using Summit.VMS.Models.Enums;
using Summit.VMS.Models.Safety;
using Summit.VMS.Services.Interfaces;

namespace Summit.VMS.Services.Safety;

public interface IIncidentWorkflowService
{
    Task<DistressIncident> RaiseAsync(int victimProfileId, Stream? triggerAudio,
        double? lat, double? lng);

    Task<DistressIncident?> RecordVerificationAsync(int incidentId, int contactId,
        VerificationDecision decision, string? officerId = null);

    Task<bool> CancelAsync(int incidentId);

    /// <summary>
    /// A concerned person raises a request. Returns the running count, the
    /// threshold, and the incident if the threshold was reached this call.
    /// </summary>
    Task<(int Count, int Threshold, DistressIncident? Incident)> RaiseConcernAsync(
        int victimProfileId, string phone, string? name, string? reason);

    /// <summary>Incidents a station-level officer must run the initial check on.</summary>
    Task<IReadOnlyList<DistressIncident>> GetPendingForOfficerAsync();

    /// <summary>Only verified+ incidents — what higher authorities are allowed to see.</summary>
    Task<IReadOnlyList<DistressIncident>> GetForHierarchyAsync();
}

public class IncidentWorkflowService : IIncidentWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly IVerificationCaller _caller;
    private readonly IVoiceMatchService _voice;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly ILogger<IncidentWorkflowService> _log;

    public IncidentWorkflowService(
        ApplicationDbContext db,
        IVerificationCaller caller,
        IVoiceMatchService voice,
        INotificationService notifications,
        IAuditService audit,
        ILogger<IncidentWorkflowService> log)
    {
        _db = db;
        _caller = caller;
        _voice = voice;
        _notifications = notifications;
        _audit = audit;
        _log = log;
    }

    public async Task<DistressIncident> RaiseAsync(int victimProfileId, Stream? triggerAudio,
        double? lat, double? lng)
    {
        var victim = await _db.Set<VictimProfile>()
            .Include(v => v.Contacts)
            .Include(v => v.VoiceSamples)
            .FirstOrDefaultAsync(v => v.Id == victimProfileId)
            ?? throw new InvalidOperationException("Victim profile not found.");

        var incident = new DistressIncident
        {
            VictimProfileId = victim.Id,
            Status = IncidentStatus.Raised,
            Latitude = lat,
            Longitude = lng,
            LocationAtUtc = (lat != null && lng != null) ? DateTime.UtcNow : null,
            RaisedAtUtc = DateTime.UtcNow
        };

        // Score the trigger voice against the enrolled samples (advisory only —
        // a low score does NOT block the alert; humans verify).
        if (triggerAudio != null && victim.VoiceSamples.Any())
        {
            var prints = victim.VoiceSamples.Select(s => s.VoiceprintId!).Where(p => p != null);
            var match = await _voice.MatchAsync(triggerAudio, prints!);
            incident.VoiceMatchScore = match.Score;
        }

        _db.Add(incident);
        await _db.SaveChangesAsync();

        // Begin the initial check: place automated calls to every emergency contact.
        incident.Status = IncidentStatus.UnderVerification;
        foreach (var contact in victim.Contacts)
        {
            var call = await _caller.PlaceVerificationCallAsync(contact, incident);
            _db.Add(new ContactVerification
            {
                DistressIncidentId = incident.Id,
                EmergencyContactId = contact.Id,
                Decision = VerificationDecision.Pending,
                CallReference = call.CallReference,
                CalledAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        await _audit.LogAsync("RaiseIncident", nameof(DistressIncident), incident.Id.ToString(),
            $"victim={victim.FullName}; score={incident.VoiceMatchScore}");

        _log.LogWarning("Distress incident {Id} raised for victim {Victim}.", incident.Id, victim.FullName);
        return incident;
    }

    public async Task<DistressIncident?> RecordVerificationAsync(int incidentId, int contactId,
        VerificationDecision decision, string? officerId = null)
    {
        var incident = await _db.Set<DistressIncident>()
            .Include(i => i.Verifications)
            .Include(i => i.VictimProfile)!.ThenInclude(v => v!.Contacts)
            .FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident == null) return null;

        var verification = incident.Verifications.FirstOrDefault(v => v.EmergencyContactId == contactId);
        if (verification == null) return incident;

        verification.Decision = decision;
        verification.RespondedAtUtc = DateTime.UtcNow;
        if (officerId != null) incident.HandlingOfficerId = officerId;

        Evaluate(incident);
        await _db.SaveChangesAsync();

        if (incident.Status == IncidentStatus.Confirmed)
            await OnConfirmedAsync(incident);

        return incident;
    }

    /// <summary>
    /// Decision rule for the initial check:
    ///   * Confirmed  -> at least one FAMILY contact AND one other contact
    ///                   confirm the person is missing.
    ///   * FalseAlarm -> two or more contacts say it was a mistake.
    /// Otherwise the incident stays UnderVerification.
    /// </summary>
    private static void Evaluate(DistressIncident incident)
    {
        var contactsById = incident.VictimProfile!.Contacts.ToDictionary(c => c.Id);

        int familyConfirms = 0, otherConfirms = 0, denials = 0;
        foreach (var v in incident.Verifications)
        {
            if (v.Decision == VerificationDecision.ConfirmedMissing)
            {
                if (contactsById.TryGetValue(v.EmergencyContactId, out var c) && c.IsFamily)
                    familyConfirms++;
                else
                    otherConfirms++;
            }
            else if (v.Decision == VerificationDecision.DeniedFalseAlarm)
            {
                denials++;
            }
        }

        if (familyConfirms >= 1 && (familyConfirms + otherConfirms) >= 2)
        {
            incident.Status = IncidentStatus.Confirmed;
            incident.ConfirmedAtUtc = DateTime.UtcNow;
        }
        else if (denials >= 2)
        {
            incident.Status = IncidentStatus.FalseAlarm;
        }
        // else: remain UnderVerification, awaiting more responses
    }

    /// <summary>
    /// Once confirmed: auto-draft the FIR (a MissingPerson case) and escalate to
    /// higher authorities. Note: legally an officer authorises an FIR — this
    /// creates a pre-filled draft for confirmation, not a substitute for that.
    /// </summary>
    private async Task OnConfirmedAsync(DistressIncident incident)
    {
        var victim = incident.VictimProfile!;

        var fir = new CaseRecord
        {
            CaseNumber = $"FIR-AUTO-{DateTime.UtcNow:yyyyMMdd}-{incident.Id:D5}",
            Title = $"Missing person (app alert): {victim.FullName}",
            Description =
                $"Auto-drafted from distress incident #{incident.Id}. " +
                $"Confirmed by emergency contacts during the initial check. " +
                $"Last known location: {incident.Latitude}, {incident.Longitude}.",
            Type = CaseType.MissingPerson,
            Status = CaseStatus.UnderInvestigation,
            Location = (incident.Latitude != null)
                ? $"{incident.Latitude},{incident.Longitude}" : null,
            DateReportedUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Add(fir);
        await _db.SaveChangesAsync();

        incident.CaseId = fir.Id;
        incident.Status = IncidentStatus.FirRegistered;
        await _db.SaveChangesAsync();

        // Escalate — now (and only now) visible to higher authorities.
        await _notifications.NotifyAsync(
            title: $"Missing-person alert confirmed: {victim.FullName}",
            message: $"Distress incident #{incident.Id} was verified by emergency contacts. " +
                     $"FIR {fir.CaseNumber} auto-drafted. Last known location: " +
                     $"{incident.Latitude?.ToString() ?? "unknown"}, {incident.Longitude?.ToString() ?? "unknown"}.",
            entityType: "DistressIncident",
            entityId: incident.Id.ToString(),
            linkPath: $"/Cases/Details/{fir.Id}",
            email: true);

        incident.Status = IncidentStatus.Escalated;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("IncidentConfirmed", nameof(DistressIncident), incident.Id.ToString(),
            $"FIR {fir.CaseNumber} auto-drafted and escalated.");
        _log.LogWarning("Incident {Id} confirmed; FIR {Fir} drafted and escalated.",
            incident.Id, fir.CaseNumber);
    }

    public async Task<(int Count, int Threshold, DistressIncident? Incident)> RaiseConcernAsync(
        int victimProfileId, string phone, string? name, string? reason)
    {
        var victim = await _db.Set<VictimProfile>()
            .Include(v => v.Contacts)
            .Include(v => v.Incidents)
            .FirstOrDefaultAsync(v => v.Id == victimProfileId)
            ?? throw new InvalidOperationException("Victim profile not found.");

        // One active request per person — upsert by phone.
        var existing = await _db.Set<ConcernRequest>()
            .FirstOrDefaultAsync(c => c.VictimProfileId == victimProfileId && c.RaisedByPhone == phone);
        if (existing == null)
            _db.Add(new ConcernRequest
            {
                VictimProfileId = victimProfileId, RaisedByPhone = phone,
                RaisedByName = name, Reason = reason
            });
        else
        {
            existing.RaisedAtUtc = DateTime.UtcNow;
            existing.Reason = reason ?? existing.Reason;
        }
        await _db.SaveChangesAsync();

        var count = await _db.Set<ConcernRequest>()
            .Where(c => c.VictimProfileId == victimProfileId)
            .Select(c => c.RaisedByPhone).Distinct().CountAsync();

        // "All the concerned people" — default to the number of nominated contacts.
        var threshold = Math.Max(2, victim.Contacts.Count);

        var active = victim.Incidents.FirstOrDefault(i =>
            i.Status is IncidentStatus.Raised or IncidentStatus.UnderVerification
                or IncidentStatus.Confirmed or IncidentStatus.FirRegistered or IncidentStatus.Escalated);

        if (count >= threshold && active == null)
        {
            var last = await _db.Set<LocationPing>()
                .Where(p => p.VictimProfileId == victimProfileId)
                .OrderByDescending(p => p.CapturedAtUtc).FirstOrDefaultAsync();

            var incident = new DistressIncident
            {
                VictimProfileId = victimProfileId,
                Status = IncidentStatus.UnderVerification,
                Latitude = last?.Latitude,
                Longitude = last?.Longitude,
                LocationAtUtc = last?.CapturedAtUtc,
                RaisedAtUtc = DateTime.UtcNow,
                Notes = $"Collectively raised by {count} concerned contacts ({reason})."
            };
            _db.Add(incident);
            await _db.SaveChangesAsync();
            await _audit.LogAsync("ConcernEscalated", nameof(DistressIncident), incident.Id.ToString(),
                $"victim={victim.FullName}; concerns={count}");
            return (count, threshold, incident);
        }

        return (count, threshold, active);
    }


    {
        var incident = await _db.Set<DistressIncident>().FindAsync(incidentId);
        if (incident == null) return false;
        if (incident.Status is IncidentStatus.Raised or IncidentStatus.UnderVerification)
        {
            incident.Status = IncidentStatus.Cancelled;
            await _db.SaveChangesAsync();
            return true;
        }
        return false; // too late to cancel once confirmed
    }

    public async Task<IReadOnlyList<DistressIncident>> GetPendingForOfficerAsync() =>
        await _db.Set<DistressIncident>()
            .Include(i => i.VictimProfile)!.ThenInclude(v => v!.Contacts)
            .Include(i => i.Verifications)
            .Where(i => i.Status == IncidentStatus.Raised
                     || i.Status == IncidentStatus.UnderVerification)
            .OrderBy(i => i.RaisedAtUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<DistressIncident>> GetForHierarchyAsync() =>
        await _db.Set<DistressIncident>()
            .Include(i => i.VictimProfile)
            .Where(i => i.Status == IncidentStatus.Confirmed
                     || i.Status == IncidentStatus.FirRegistered
                     || i.Status == IncidentStatus.Escalated
                     || i.Status == IncidentStatus.Resolved)
            .OrderByDescending(i => i.ConfirmedAtUtc)
            .ToListAsync();
}
