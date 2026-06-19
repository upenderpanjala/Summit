using System.ComponentModel.DataAnnotations;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.DTOs;

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserId,
    string Email,
    string FullName,
    IEnumerable<string> Roles);

public record VictimDto(
    int Id,
    string ReferenceNumber,
    string FirstName,
    string LastName,
    Gender Gender,
    DateTime? DateOfBirth,
    string? NationalId,
    string? ContactNumber,
    string? Email,
    string? Address,
    string? City,
    string? State,
    string? Notes,
    int? CaseId,
    string? CaseNumber);

public record VictimSummaryDto(
    int Id,
    string ReferenceNumber,
    string FullName,
    Gender Gender,
    string? City,
    int? CaseId,
    string? CaseNumber);

public class VictimCreateUpdateDto
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Unspecified;
    public DateTime? DateOfBirth { get; set; }
    [StringLength(50)] public string? NationalId { get; set; }
    [StringLength(30)] public string? ContactNumber { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(400)] public string? Address { get; set; }
    [StringLength(120)] public string? City { get; set; }
    [StringLength(120)] public string? State { get; set; }
    [StringLength(4000)] public string? Notes { get; set; }
    public int? CaseId { get; set; }
}

public record CaseSummaryDto(
    int Id,
    string CaseNumber,
    string Title,
    CaseType Type,
    CaseStatus Status,
    DateTime DateReportedUtc,
    int VictimCount);
