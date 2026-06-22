using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Summit.VMS.Models.Safety;

namespace Summit.VMS.Data.Configurations;

public class VictimProfileConfiguration : IEntityTypeConfiguration<VictimProfile>
{
    public void Configure(EntityTypeBuilder<VictimProfile> b)
    {
        b.ToTable("VictimProfiles");
        b.HasKey(v => v.Id);
        b.HasIndex(v => v.Mobile);
        b.Property(v => v.Gender).HasConversion<int>();
        b.Property(v => v.VerificationStatus).HasConversion<int>();
        b.Property(v => v.RegistrationStatus).HasConversion<int>();
        b.Property(v => v.State).HasConversion<int?>();

        b.HasMany(v => v.Contacts).WithOne(c => c.VictimProfile!)
            .HasForeignKey(c => c.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.VoiceSamples).WithOne(s => s.VictimProfile!)
            .HasForeignKey(s => s.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Incidents).WithOne(i => i.VictimProfile!)
            .HasForeignKey(i => i.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> b)
    {
        b.ToTable("EmergencyContacts");
        b.HasKey(c => c.Id);
        b.Property(c => c.Relation).HasConversion<int>();
        b.Ignore(c => c.IsFamily); // computed
    }
}

public class DistressVoiceSampleConfiguration : IEntityTypeConfiguration<DistressVoiceSample>
{
    public void Configure(EntityTypeBuilder<DistressVoiceSample> b)
    {
        b.ToTable("DistressVoiceSamples");
        b.HasKey(s => s.Id);
    }
}

public class DistressIncidentConfiguration : IEntityTypeConfiguration<DistressIncident>
{
    public void Configure(EntityTypeBuilder<DistressIncident> b)
    {
        b.ToTable("DistressIncidents");
        b.HasKey(i => i.Id);
        b.HasIndex(i => i.Status);
        b.Property(i => i.Status).HasConversion<int>();
        b.Ignore(i => i.IsVisibleToHierarchy); // computed

        b.HasMany(i => i.Verifications).WithOne(v => v.DistressIncident!)
            .HasForeignKey(v => v.DistressIncidentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContactVerificationConfiguration : IEntityTypeConfiguration<ContactVerification>
{
    public void Configure(EntityTypeBuilder<ContactVerification> b)
    {
        b.ToTable("ContactVerifications");
        b.HasKey(v => v.Id);
        b.Property(v => v.Decision).HasConversion<int>();

        // Second FK to EmergencyContact must NOT cascade (avoids multiple
        // cascade paths into ContactVerifications, which SQL Server rejects).
        b.HasOne(v => v.EmergencyContact).WithMany()
            .HasForeignKey(v => v.EmergencyContactId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PoliceAppRegistrationConfiguration : IEntityTypeConfiguration<PoliceAppRegistration>
{
    public void Configure(EntityTypeBuilder<PoliceAppRegistration> b)
    {
        b.ToTable("PoliceAppRegistrations");
        b.HasKey(p => p.Id);
        b.HasIndex(p => p.PoliceId).IsUnique();
        b.Property(p => p.Rank).HasConversion<int>();
        b.Property(p => p.State).HasConversion<int?>();
    }
}

public class GuardianAppRegistrationConfiguration : IEntityTypeConfiguration<GuardianAppRegistration>
{
    public void Configure(EntityTypeBuilder<GuardianAppRegistration> b)
    {
        b.ToTable("GuardianAppRegistrations");
        b.HasKey(g => g.Id);
        b.HasIndex(g => new { g.VictimProfileId, g.Phone });
        b.Property(g => g.Relation).HasConversion<int>();
        b.Property(g => g.VerificationStatus).HasConversion<int>();
        b.HasOne(g => g.VictimProfile).WithMany()
            .HasForeignKey(g => g.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LocationPingConfiguration : IEntityTypeConfiguration<LocationPing>
{
    public void Configure(EntityTypeBuilder<LocationPing> b)
    {
        b.ToTable("LocationPings");
        b.HasKey(p => p.Id);
        b.HasIndex(p => new { p.VictimProfileId, p.CapturedAtUtc });
        b.HasOne(p => p.VictimProfile).WithMany()
            .HasForeignKey(p => p.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConcernRequestConfiguration : IEntityTypeConfiguration<ConcernRequest>
{
    public void Configure(EntityTypeBuilder<ConcernRequest> b)
    {
        b.ToTable("ConcernRequests");
        b.HasKey(c => c.Id);
        b.HasIndex(c => new { c.VictimProfileId, c.RaisedByPhone });
        b.HasOne(c => c.VictimProfile).WithMany()
            .HasForeignKey(c => c.VictimProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
