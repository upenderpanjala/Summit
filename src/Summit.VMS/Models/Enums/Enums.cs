namespace Summit.VMS.Models.Enums;

/// <summary>
/// Canonical role names. Kept as constants so they can be used in
/// [Authorize(Roles = ...)] attributes, policy definitions and seeding.
/// </summary>
public static class AppRoles
{
    /// <summary>Full control: users, cases, victims, configuration.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Front-line officer: create/edit cases and victim records.</summary>
    public const string Investigator = "Investigator";

    /// <summary>Senior police ranks. READ-ONLY access to victim details.</summary>
    public const string PoliceHierarchy = "PoliceHierarchy";

    /// <summary>Home Minister oversight. READ-ONLY access to victim details.</summary>
    public const string HomeMinister = "HomeMinister";

    public static readonly string[] All =
    {
        Administrator, Investigator, PoliceHierarchy, HomeMinister
    };

    /// <summary>Roles that may only ever view (never mutate) victim data.</summary>
    public static readonly string[] ViewOnly =
    {
        PoliceHierarchy, HomeMinister
    };
}

/// <summary>Police rank ladder used on officer accounts.</summary>
public enum PoliceRank
{
    Constable = 0,
    HeadConstable = 1,
    SubInspector = 2,
    Inspector = 3,
    DeputySuperintendent = 4,
    Superintendent = 5,
    DeputyInspectorGeneral = 6,
    InspectorGeneral = 7,
    DirectorGeneral = 8
}

public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

public enum CaseType
{
    Other = 0,
    Theft = 1,
    Assault = 2,
    Homicide = 3,
    Fraud = 4,
    DomesticViolence = 5,
    Cybercrime = 6,
    MissingPerson = 7,
    Trafficking = 8
}

public enum CaseStatus
{
    Open = 0,
    UnderInvestigation = 1,
    Suspended = 2,
    Closed = 3,
    Archived = 4
}
