using System.ComponentModel.DataAnnotations;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.Models.Safety;

/// <summary>
/// A person who self-registers through the victim mobile app.
///
/// Identity is established with a Summit verification token (see ISummitVerifier).
/// The token is an opaque reference — no national ID number is collected or stored.
/// </summary>
public class VictimProfile
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Unspecified;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Required, Phone, StringLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(400)]
    public string? Address { get; set; }

    [StringLength(120)]
    public string? City { get; set; }

    /// <summary>State this victim falls under (Telangana / Andhra Pradesh).</summary>
    public IndianState? State { get; set; }

    /// <summary>District within the state — used to route the alert to the right jurisdiction.</summary>
    [StringLength(120)]
    public string? District { get; set; }

    // ---- Parent / guardian (captured at registration) ----
    [StringLength(150)]
    public string? GuardianName { get; set; }

    [Phone, StringLength(20)]
    public string? GuardianPhone { get; set; }

    // ---- Summit verification (opaque token only) ----
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotVerified;

    /// <summary>Opaque token issued on successful verification. Not a national ID.</summary>
    [StringLength(128)]
    public string? VerificationToken { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }

    // ---- OTP sent to the parent/guardian phone (prototype: stored transiently) ----
    [StringLength(10)]
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresUtc { get; set; }
    public DateTime? OtpSentAtUtc { get; set; }
    public int OtpAttempts { get; set; }

    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.PendingVerification;

    /// <summary>Identity link to the app login (ASP.NET Identity user id), if used.</summary>
    [StringLength(450)]
    public string? IdentityUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<EmergencyContact> Contacts { get; set; } = new List<EmergencyContact>();
    public ICollection<DistressVoiceSample> VoiceSamples { get; set; } = new List<DistressVoiceSample>();
    public ICollection<DistressIncident> Incidents { get; set; } = new List<DistressIncident>();
}

/// <summary>
/// A guardian/sibling/friend the victim nominates at registration. Each must be
/// informed by an automated call and (ideally) consent before going live.
/// </summary>
public class EmergencyContact
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public ContactRelation Relation { get; set; } = ContactRelation.Other;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>True for parents/guardians — at least one must confirm to validate a case.</summary>
    public bool IsFamily => Relation is ContactRelation.Mother
        or ContactRelation.Father or ContactRelation.Guardian or ContactRelation.Sibling;

    /// <summary>Set when the informational/consent call was placed at registration.</summary>
    public DateTime? InformedAtUtc { get; set; }
    public bool Consented { get; set; }
}

/// <summary>
/// A reference to a stored voice enrolment sample (3 collected at registration).
/// The audio itself lives in the document/object store; we keep a pointer plus a
/// voiceprint id produced by the matching service.
/// </summary>
public class DistressVoiceSample
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    [Required, StringLength(400)]
    public string StoredPath { get; set; } = string.Empty;

    /// <summary>Opaque voiceprint id from the matching service (not raw audio).</summary>
    [StringLength(128)]
    public string? VoiceprintId { get; set; }

    public int Ordinal { get; set; } // 1..3
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A distress event: created when the victim plays/sends their distress voice (or
/// presses the panic button). Visibility to higher authorities is staged on Status.
/// </summary>
public class DistressIncident
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Raised;

    // ---- Trigger evidence ----
    [StringLength(400)]
    public string? TriggerVoicePath { get; set; }

    /// <summary>0..1 confidence that the trigger voice matches the enrolled samples.</summary>
    public double? VoiceMatchScore { get; set; }

    // ---- Last known location at trigger time ----
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LocationAtUtc { get; set; }

    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>Station-level officer who ran the initial check.</summary>
    [StringLength(450)]
    public string? HandlingOfficerId { get; set; }

    /// <summary>FIR auto-drafted from this incident, if any.</summary>
    public int? CaseId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public ICollection<ContactVerification> Verifications { get; set; } = new List<ContactVerification>();

    /// <summary>True once verified — the point at which higher authorities may see it.</summary>
    public bool IsVisibleToHierarchy =>
        Status is IncidentStatus.Confirmed or IncidentStatus.FirRegistered
            or IncidentStatus.Escalated or IncidentStatus.Resolved;
}

/// <summary>Records the automated initial-check call to one emergency contact.</summary>
public class ContactVerification
{
    public int Id { get; set; }

    public int DistressIncidentId { get; set; }
    public DistressIncident? DistressIncident { get; set; }

    public int EmergencyContactId { get; set; }
    public EmergencyContact? EmergencyContact { get; set; }

    public VerificationDecision Decision { get; set; } = VerificationDecision.Pending;

    /// <summary>Provider call reference (IVR/voice provider), for audit.</summary>
    [StringLength(128)]
    public string? CallReference { get; set; }

    public DateTime? CalledAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}

/// <summary>
/// A police officer's self-registration from the police mobile app (subject to
/// approval before the account becomes active).
/// </summary>
public class PoliceAppRegistration
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string PoliceId { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public PoliceRank Rank { get; set; } = PoliceRank.Constable;

    /// <summary>Jurisdiction the officer serves.</summary>
    public IndianState? State { get; set; }

    [StringLength(120)]
    public string? District { get; set; }

    /// <summary>Police zone / commissionerate zone (free text — varies by city).</summary>
    [StringLength(120)]
    public string? Zone { get; set; }

    public int? PoliceStationId { get; set; }

    [StringLength(150)]
    public string? StationName { get; set; }

    [Phone, StringLength(20)]
    public string? Mobile { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    public bool IsApproved { get; set; }

    [StringLength(450)]
    public string? IdentityUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// ===========================================================================
//  Parent / concerned-person app
// ===========================================================================

/// <summary>
/// A parent or other concerned person's app account, linked to a victim AFTER she
/// has registered. Only a number the victim nominated (guardian or an emergency
/// contact) may register, so strangers cannot attach themselves to a profile.
/// </summary>
public class GuardianAppRegistration
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public ContactRelation Relation { get; set; } = ContactRelation.Guardian;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>True only if the phone matches a number the victim nominated.</summary>
    public bool IsApproved { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotVerified;

    [StringLength(128)]
    public string? VerificationToken { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A location share from the victim app, so approved parents can see her live
/// position. Requires the victim's explicit opt-in (and guardian consent if a
/// minor). OnRoute is the client-side result of matching against a chosen route.
/// </summary>
public class LocationPing
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>False when the app detects she has deviated from the expected route.</summary>
    public bool OnRoute { get; set; } = true;

    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A concerned person proactively raising a request about a victim (e.g. because
/// she went off-route or is unreachable). When enough distinct concerned people
/// raise one, the system creates an incident and routes it to the police.
/// </summary>
public class ConcernRequest
{
    public int Id { get; set; }

    public int VictimProfileId { get; set; }
    public VictimProfile? VictimProfile { get; set; }

    [Required, Phone, StringLength(20)]
    public string RaisedByPhone { get; set; } = string.Empty;

    [StringLength(150)]
    public string? RaisedByName { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;
}
