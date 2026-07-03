using GoldKiosk.Infrastructure.Entities.Monitor;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Monitor;

/// <summary>Exception log configuration.</summary>
public sealed class ExceptionLogConfiguration : IEntityTypeConfiguration<ExceptionLog>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<ExceptionLog> builder)
    {
        builder.ToTable("exception_logs", "monitor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.ExceptionName).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.Severity).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OccurredAt })
            .HasDatabaseName("ix_exception_logs__tenant_time")
            .IsDescending(false, true);

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
