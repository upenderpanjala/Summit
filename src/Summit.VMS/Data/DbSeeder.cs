using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Models.Entities;
using Summit.VMS.Models.Enums;

namespace Summit.VMS.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleMgr = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // Prefer migrations (recommended). If none have been added yet, fall
        // back to EnsureCreated so the app still runs out of the box.
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        // ---- Roles ----
        foreach (var role in AppRoles.All)
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new ApplicationRole(role)
                {
                    Description = role switch
                    {
                        AppRoles.Administrator => "Full platform control.",
                        AppRoles.Investigator => "Manages cases and victim records.",
                        AppRoles.PoliceHierarchy => "Senior ranks. View-only victim access.",
                        AppRoles.HomeMinister => "Ministerial oversight. View-only victim access.",
                        _ => null
                    }
                });
        }

        // ---- Admin from configuration ----
        var adminCfg = config.GetSection("SeedAdmin");
        await EnsureUserAsync(userMgr, adminCfg["Email"]!, adminCfg["Password"]!,
            adminCfg["FullName"]!, AppRoles.Administrator, mobile: "+1-555-0100");

        // ---- Demo accounts (change/remove in production) ----
        await EnsureUserAsync(userMgr, "investigator@summit.gov", "Invest#12345",
            "Inspector J. Rao", AppRoles.Investigator, PoliceRank.Inspector, "BDG-1001", "+1-555-0101");

        await EnsureUserAsync(userMgr, "dgp@summit.gov", "Police#12345",
            "DGP A. Menon", AppRoles.PoliceHierarchy, PoliceRank.DirectorGeneral, "BDG-0001", "+1-555-0102");

        await EnsureUserAsync(userMgr, "minister@summit.gov", "Minister#12345",
            "Hon. Home Minister", AppRoles.HomeMinister, mobile: "+1-555-0103");

        // ---- Sample reference data ----
        if (!await db.PoliceStations.AnyAsync())
        {
            db.PoliceStations.AddRange(
                new PoliceStation { Name = "Central City Station", Code = "CCS", District = "Central", State = "NY" },
                new PoliceStation { Name = "Riverside Station", Code = "RVS", District = "Riverside", State = "NY" });
            await db.SaveChangesAsync();
        }

        if (!await db.Cases.AnyAsync())
        {
            var station = await db.PoliceStations.FirstAsync();
            var officer = await userMgr.FindByEmailAsync("investigator@summit.gov");

            var c1 = new CaseRecord
            {
                CaseNumber = "FIR-2026-0001",
                Title = "Residential burglary on Elm Street",
                Description = "Forced entry reported by homeowner.",
                Type = CaseType.Theft,
                Status = CaseStatus.UnderInvestigation,
                Location = "Elm Street, Central",
                PoliceStationId = station.Id,
                AssignedOfficerId = officer?.Id,
                DateReportedUtc = DateTime.UtcNow.AddDays(-12)
            };
            db.Cases.Add(c1);
            await db.SaveChangesAsync();

            db.Victims.AddRange(
                new Victim
                {
                    ReferenceNumber = "L99",
                    FirstName = "Lena", LastName = "Hartwell",
                    Gender = Gender.Female, City = "Central", State = "NY",
                    ContactNumber = "+1-555-0199",
                    Notes = "Primary complainant. Property loss documented.",
                    CaseId = c1.Id
                },
                new Victim
                {
                    ReferenceNumber = "L100",
                    FirstName = "Marcus", LastName = "Doyle",
                    Gender = Gender.Male, City = "Central", State = "NY",
                    CaseId = c1.Id
                });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userMgr,
        string email, string password, string fullName, string role,
        PoliceRank? rank = null, string? badge = null, string? mobile = null)
    {
        var user = await userMgr.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Rank = rank,
                BadgeNumber = badge,
                Mobile = mobile,
                IsActive = true
            };
            var result = await userMgr.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception(
                    $"Failed to seed {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userMgr.IsInRoleAsync(user, role))
            await userMgr.AddToRoleAsync(user, role);
    }
}
