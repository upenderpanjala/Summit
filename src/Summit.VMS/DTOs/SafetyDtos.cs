using System.ComponentModel.DataAnnotations;
using Summit.VMS.Models.Enums;
using Summit.VMS.Models.Safety;
using Summit.VMS.Validation;

namespace Summit.VMS.DTOs;

// ---------- Victim app ----------

public class VictimRegisterDto
{
    [Required, PersonName] public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Unspecified;
    public DateTime? DateOfBirth { get; set; }
    [Required, IndianMobile] public string Mobile { get; set; } = string.Empty;
    [EmailAddress] public string? Email { get; set; }
    [Required, StringLength(200, MinimumLength = 5)] public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    [Required] public IndianState State { get; set; } = IndianState.Telangana;
    [Required] public string District { get; set; } = string.Empty;

    // Parent / guardian
    [Required, PersonName] public string GuardianName { get; set; } = string.Empty;
    [Required, IndianMobile] public string GuardianPhone { get; set; } = string.Empty;
}

public class OtpVerifyDto
{
    [Required, OtpCode] public string Code { get; set; } = string.Empty;
}

public class EmergencyContactDto
{
    [Required, PersonName] public string Name { get; set; } = string.Empty;
    public ContactRelation Relation { get; set; } = ContactRelation.Other;
    [Required, IndianMobile] public string Phone { get; set; } = string.Empty;
}

public record VictimProfileStateDto(
    int Id, string FullName, RegistrationStatus RegistrationStatus,
    VerificationStatus VerificationStatus, int ContactCount, int VoiceSampleCount,
    IncidentStatus? ActiveIncidentStatus);

public record IncidentStateDto(
    int Id, IncidentStatus Status, double? VoiceMatchScore,
    double? Latitude, double? Longitude, DateTime RaisedAtUtc, int? CaseId);

// ---------- Police app ----------

public class PoliceRegisterDto
{
    [Required, StringLength(50)] public string PoliceId { get; set; } = string.Empty;
    [Required, PersonName] public string FullName { get; set; } = string.Empty;
    public PoliceRank Rank { get; set; } = PoliceRank.Constable;
    [Required] public IndianState State { get; set; } = IndianState.Telangana;
    [Required] public string District { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public int? PoliceStationId { get; set; }
    public string? StationName { get; set; }
    [IndianMobile(AllowEmpty = true)] public string? Mobile { get; set; }
    [EmailAddress] public string? Email { get; set; }
}

public class VerificationDecisionDto
{
    [Required] public int ContactId { get; set; }
    public VerificationDecision Decision { get; set; } = VerificationDecision.Pending;
}

public record PendingIncidentDto(
    int Id, string VictimName, string VictimMobile, IncidentStatus Status,
    string? GuardianName, string? GuardianPhone, string? District, string? StateName,
    double? Latitude, double? Longitude, DateTime RaisedAtUtc,
    IEnumerable<ContactCheckDto> Contacts);

public record ContactCheckDto(
    int ContactId, string Name, ContactRelation Relation, string Phone,
    VerificationDecision Decision);

public record ConfirmedIncidentDto(
    int Id, string VictimName, string VictimMobile, IncidentStatus Status,
    double? Latitude, double? Longitude, DateTime? ConfirmedAtUtc, int? CaseId);

// ---------- Parent / concerned-person app ----------

public class ParentRegisterDto
{
    [Required, IndianMobile] public string VictimMobile { get; set; } = string.Empty;
    [Required, PersonName] public string Name { get; set; } = string.Empty;
    public ContactRelation Relation { get; set; } = ContactRelation.Guardian;
    [Required, IndianMobile] public string Phone { get; set; } = string.Empty;
}

public record ParentLinkDto(int VictimProfileId, string VictimName, bool Approved, string Message);

public class LocationPingDto
{
    [Required] public double Lat { get; set; }
    [Required] public double Lng { get; set; }
    public bool OnRoute { get; set; } = true;
}

public class ConcernDto
{
    [Required, IndianMobile] public string Phone { get; set; } = string.Empty;
    [StringLength(150)] public string? Name { get; set; }
    [StringLength(300)] public string? Reason { get; set; }
}

public record LiveLocationDto(
    double? Lat, double? Lng, bool OnRoute, DateTime? At,
    IncidentStatus? IncidentStatus, int ConcernCount, int ConcernThreshold);
