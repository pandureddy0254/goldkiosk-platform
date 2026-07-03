using GoldKiosk.Infrastructure.Entities.Kiosk;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Kiosk security token configuration.</summary>
public sealed class KioskSecurityTokenConfiguration : IEntityTypeConfiguration<KioskSecurityToken>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<KioskSecurityToken> builder)
    {
        builder.ToTable("kiosk_security_tokens", "kiosk");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.TokenHash).HasColumnType("bytea").IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .HasDatabaseName("uq_kiosk_security_tokens__hash")
            .IsUnique();

        builder.HasIndex(x => x.KioskId)
            .HasDatabaseName("ix_kiosk_security_tokens__active")
            .HasFilter("revoked_at IS NULL");

        // Shadow FK so SaveChanges orders inserts (kiosks → kiosk_security_tokens).
        builder.HasOne<Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Shadow FK to users (issued_by_user_id).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
