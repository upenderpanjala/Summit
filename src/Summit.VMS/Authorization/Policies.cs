using Microsoft.AspNetCore.Authorization;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.Authorization;

public static class Policies
{
    /// <summary>Anyone authenticated in one of the four roles may VIEW victim details.</summary>
    public const string ViewVictims = "ViewVictims";

    /// <summary>Only Administrator and Investigator may create/edit/delete victims.</summary>
    public const string ManageVictims = "ManageVictims";

    /// <summary>View case records.</summary>
    public const string ViewCases = "ViewCases";

    /// <summary>Create/edit/delete cases.</summary>
    public const string ManageCases = "ManageCases";

    /// <summary>User administration (Administrator only).</summary>
    public const string ManageUsers = "ManageUsers";

    public static void AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ViewVictims, p => p.RequireRole(
                AppRoles.Administrator, AppRoles.Investigator,
                AppRoles.PoliceHierarchy, AppRoles.HomeMinister));

            options.AddPolicy(ManageVictims, p => p.RequireRole(
                AppRoles.Administrator, AppRoles.Investigator));

            options.AddPolicy(ViewCases, p => p.RequireRole(
                AppRoles.Administrator, AppRoles.Investigator,
                AppRoles.PoliceHierarchy));

            options.AddPolicy(ManageCases, p => p.RequireRole(
                AppRoles.Administrator, AppRoles.Investigator));

            options.AddPolicy(ManageUsers, p => p.RequireRole(
                AppRoles.Administrator));
        });
    }
}
