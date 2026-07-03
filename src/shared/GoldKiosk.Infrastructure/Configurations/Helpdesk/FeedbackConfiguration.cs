using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Helpdesk;

/// <summary>Feedback configuration.</summary>
public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedback", "helpdesk");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FunctionName).IsRequired();
        builder.Property(x => x.FeedbackType).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("ix_feedback__tenant_time")
            .IsDescending(false, true);

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Entities.Customer.Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
