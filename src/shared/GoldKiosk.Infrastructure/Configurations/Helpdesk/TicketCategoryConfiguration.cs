using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Helpdesk;

/// <summary>Ticket category configuration.</summary>
public sealed class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("ticket_categories", "helpdesk");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Name).IsRequired();

        builder.HasIndex(x => x.Code)
            .HasDatabaseName("uq_ticket_categories__code")
            .IsUnique();
    }
}
