using System.ComponentModel.DataAnnotations;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class VictimFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(40), Display(Name = "Reference No.")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Unspecified;

    [DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(50), Display(Name = "National ID")]
    public string? NationalId { get; set; }

    [StringLength(30), Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(400)]
    public string? Address { get; set; }

    [StringLength(120)]
    public string? City { get; set; }

    [StringLength(120)]
    public string? State { get; set; }

    [StringLength(4000), Display(Name = "Notes / statement")]
    public string? Notes { get; set; }

    [Display(Name = "Linked case")]
    public int? CaseId { get; set; }
}

public class CaseFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(40), Display(Name = "Case number")]
    public string CaseNumber { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public CaseType Type { get; set; } = CaseType.Other;
    public CaseStatus Status { get; set; } = CaseStatus.Open;

    [StringLength(300)]
    public string? Location { get; set; }

    [Display(Name = "Police station")]
    public int? PoliceStationId { get; set; }

    [Display(Name = "Assigned officer")]
    public string? AssignedOfficerId { get; set; }
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? BadgeNumber { get; set; }
    public bool IsActive { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
