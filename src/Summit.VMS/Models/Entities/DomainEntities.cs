using System.ComponentModel.DataAnnotations;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.Models.Entities;

public class PoliceStation
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(200)]
    public string? District { get; set; }

    [StringLength(200)]
    public string? State { get; set; }

    public ICollection<CaseRecord> Cases { get; set; } = new List<CaseRecord>();
}

public class CaseRecord
{
    public int Id { get; set; }

    /// <summary>Human-readable case reference, e.g. "FIR-2026-0042".</summary>
    [Required, StringLength(40)]
    public string CaseNumber { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public CaseType Type { get; set; } = CaseType.Other;
    public CaseStatus Status { get; set; } = CaseStatus.Open;

    [StringLength(300)]
    public string? Location { get; set; }

    public DateTime DateReportedUtc { get; set; } = DateTime.UtcNow;

    public int? PoliceStationId { get; set; }
    public PoliceStation? PoliceStation { get; set; }

    /// <summary>Investigator who owns the case.</summary>
    public string? AssignedOfficerId { get; set; }
    public ApplicationUser? AssignedOfficer { get; set; }

    public ICollection<Victim> Victims { get; set; } = new List<Victim>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Victim record. This is the entity that PoliceHierarchy and HomeMinister
/// roles are permitted to VIEW but never modify.
/// </summary>
public class Victim
{
    public int Id { get; set; }

    /// <summary>Internal victim reference label (e.g. "L99").</summary>
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Unspecified;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(50)]
    public string? NationalId { get; set; }

    [Phone, StringLength(30), Display(Name = "Mobile number")]
    public string? ContactNumber { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(400)]
    public string? Address { get; set; }

    [StringLength(120)]
    public string? City { get; set; }

    [StringLength(120)]
    public string? State { get; set; }

    /// <summary>Sensitive narrative / injuries / statement summary.</summary>
    [StringLength(4000)]
    public string? Notes { get; set; }

    public int? CaseId { get; set; }
    public CaseRecord? Case { get; set; }

    public ICollection<VictimDocument> Documents { get; set; } = new List<VictimDocument>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Immutable audit trail. Records every read/write touch on victim data so
/// that view-only access by senior ranks and the Home Minister is accountable.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = string.Empty; // e.g. "ViewVictim", "CreateVictim"

    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty; // e.g. "Victim"

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(2000)]
    public string? Details { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }
}

/// <summary>
/// A file (statement, medical report, evidence photo, FIR copy, ...) attached
/// to a victim record. Uploaded by an Investigator/Administrator; viewable by
/// every role that can see victim details.
/// </summary>
public class VictimDocument
{
    public int Id { get; set; }

    public int VictimId { get; set; }
    public Victim? Victim { get; set; }

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Relative path on the server's document store.</summary>
    [Required, StringLength(400)]
    public string StoredPath { get; set; } = string.Empty;

    [StringLength(450)]
    public string? UploadedById { get; set; }

    [StringLength(256)]
    public string? UploadedByName { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A system-wide notification. Created when significant events occur (e.g. a new
/// victim record is inserted) and shown to every signed-in user.
/// </summary>
public class Notification
{
    public long Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Message { get; set; }

    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty; // e.g. "Victim"

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(256)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
