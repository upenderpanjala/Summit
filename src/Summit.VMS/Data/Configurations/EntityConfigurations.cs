using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Summit.VMS.Models.Entities;

namespace Summit.VMS.Data.Configurations;

public class VictimConfiguration : IEntityTypeConfiguration<Victim>
{
    public void Configure(EntityTypeBuilder<Victim> b)
    {
        b.ToTable("Victims");
        b.HasKey(v => v.Id);
        b.HasIndex(v => v.ReferenceNumber).IsUnique();
        b.Property(v => v.Gender).HasConversion<int>();

        b.HasOne(v => v.Case)
            .WithMany(c => c.Victims)
            .HasForeignKey(v => v.CaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CaseConfiguration : IEntityTypeConfiguration<CaseRecord>
{
    public void Configure(EntityTypeBuilder<CaseRecord> b)
    {
        b.ToTable("Cases");
        b.HasKey(c => c.Id);
        b.HasIndex(c => c.CaseNumber).IsUnique();
        b.Property(c => c.Type).HasConversion<int>();
        b.Property(c => c.Status).HasConversion<int>();

        b.HasOne(c => c.AssignedOfficer)
            .WithMany(u => u.AssignedCases)
            .HasForeignKey(c => c.AssignedOfficerId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(c => c.PoliceStation)
            .WithMany(s => s.Cases)
            .HasForeignKey(c => c.PoliceStationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PoliceStationConfiguration : IEntityTypeConfiguration<PoliceStation>
{
    public void Configure(EntityTypeBuilder<PoliceStation> b)
    {
        b.ToTable("PoliceStations");
        b.HasKey(s => s.Id);
        b.HasIndex(s => s.Code).IsUnique(false);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(a => a.Id);
        b.HasIndex(a => a.TimestampUtc);
        b.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.FullName).HasMaxLength(200);
        b.Property(u => u.Mobile).HasMaxLength(30);
        b.Property(u => u.BadgeNumber).HasMaxLength(50);
        // PoliceRank? maps to int? by EF Core convention — no explicit conversion needed.

        b.HasOne(u => u.PoliceStation)
            .WithMany()
            .HasForeignKey(u => u.PoliceStationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class VictimDocumentConfiguration : IEntityTypeConfiguration<VictimDocument>
{
    public void Configure(EntityTypeBuilder<VictimDocument> b)
    {
        b.ToTable("VictimDocuments");
        b.HasKey(d => d.Id);
        b.HasIndex(d => d.VictimId);

        b.HasOne(d => d.Victim)
            .WithMany(v => v.Documents)
            .HasForeignKey(d => d.VictimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(n => n.Id);
        b.HasIndex(n => n.CreatedAtUtc);
    }
}
