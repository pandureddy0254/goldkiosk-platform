using GoldKiosk.Infrastructure.Entities.Ops;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Ticket status history configuration.</summary>
public sealed class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.ToTable("ticket_status_history", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.TicketKind).IsRequired();
        builder.Property(x => x.NewStatus).IsRequired();

        builder.HasIndex(x => new { x.TicketKind, x.TicketId, x.ChangedAt })
            .HasDatabaseName("ix_ticket_status_history__ticket");

        // Shadow FK to users (changed_by_user_id).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
