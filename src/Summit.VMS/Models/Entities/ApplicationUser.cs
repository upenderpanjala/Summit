using Microsoft.AspNetCore.Identity;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.Models.Entities;

/// <summary>
/// Application user. Extends the Identity user with police-specific fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Mobile contact number for the officer / official.</summary>
    public string? Mobile { get; set; }

    /// <summary>Officer badge / service number. Null for non-officer accounts (e.g. Home Minister).</summary>
    public string? BadgeNumber { get; set; }

    public PoliceRank? Rank { get; set; }

    /// <summary>Optional posting / station.</summary>
    public int? PoliceStationId { get; set; }
    public PoliceStation? PoliceStation { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CaseRecord> AssignedCases { get; set; } = new List<CaseRecord>();
}

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
