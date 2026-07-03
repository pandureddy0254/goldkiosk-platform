using GoldKiosk.Infrastructure.Entities.Ops;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Technician configuration.</summary>
public sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.ToTable("technicians", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.EmailEnc).HasColumnType("bytea");
        builder.Property(x => x.MobileEnc).HasColumnType("bytea");

        builder.HasIndex(x => x.ClusterId)
            .HasDatabaseName("ix_technicians__cluster")
            .HasFilter("is_active = true");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TechnicianCluster>()
            .WithMany()
            .HasForeignKey(x => x.ClusterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
