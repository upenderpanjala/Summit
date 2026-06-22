using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Models.Entities;
using Summit.VMS.Models.Safety;

namespace Summit.VMS.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Victim> Victims => Set<Victim>();
    public DbSet<CaseRecord> Cases => Set<CaseRecord>();
    public DbSet<PoliceStation> PoliceStations => Set<PoliceStation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<VictimDocument> VictimDocuments => Set<VictimDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // ---- Safety / distress module (mobile apps) ----
    public DbSet<VictimProfile> VictimProfiles => Set<VictimProfile>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<DistressVoiceSample> DistressVoiceSamples => Set<DistressVoiceSample>();
    public DbSet<DistressIncident> DistressIncidents => Set<DistressIncident>();
    public DbSet<ContactVerification> ContactVerifications => Set<ContactVerification>();
    public DbSet<PoliceAppRegistration> PoliceAppRegistrations => Set<PoliceAppRegistration>();
    public DbSet<GuardianAppRegistration> GuardianAppRegistrations => Set<GuardianAppRegistration>();
    public DbSet<LocationPing> LocationPings => Set<LocationPing>();
    public DbSet<ConcernRequest> ConcernRequests => Set<ConcernRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
