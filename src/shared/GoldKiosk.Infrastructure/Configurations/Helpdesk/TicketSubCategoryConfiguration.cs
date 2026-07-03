using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Helpdesk;

/// <summary>Ticket sub category configuration.</summary>
public sealed class TicketSubCategoryConfiguration : IEntityTypeConfiguration<TicketSubCategory>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<TicketSubCategory> builder)
    {
        builder.ToTable("ticket_sub_categories", "helpdesk");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasColumnType("citext").IsRequired();
        builder.Property(x => x.Name).IsRequired();

        builder.HasIndex(x => new { x.ParentCategoryId, x.Code })
            .HasDatabaseName("uq_ticket_sub_categories__parent_code")
            .IsUnique();

        // Shadow FK so SaveChanges orders inserts (ticket_categories → ticket_sub_categories).
        builder.HasOne<TicketCategory>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
