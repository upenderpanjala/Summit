using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Summit.VMS.Models.Entities;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
