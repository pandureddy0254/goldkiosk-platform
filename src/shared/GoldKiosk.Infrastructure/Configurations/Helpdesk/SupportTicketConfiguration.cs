using GoldKiosk.Infrastructure.Entities.Helpdesk;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Helpdesk;

/// <summary>Support ticket configuration.</summary>
public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets", "helpdesk");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .HasDatabaseName("uq_support_tickets__tenant_code")
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_support_tickets__tenant_status");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<TicketCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TicketSubCategory>()
            .WithMany()
            .HasForeignKey(x => x.SubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ReopenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
