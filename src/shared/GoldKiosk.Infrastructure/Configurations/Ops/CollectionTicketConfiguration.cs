using GoldKiosk.Infrastructure.Entities.Ops;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Ops;

/// <summary>Collection ticket configuration.</summary>
public sealed class CollectionTicketConfiguration : IEntityTypeConfiguration<CollectionTicket>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<CollectionTicket> builder)
    {
        builder.ToTable("collection_tickets", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.AmountCollected).HasColumnType("numeric");
        builder.Property(x => x.CurrencyCode).IsRequired();

        builder.HasIndex(x => x.CollectionRunId)
            .HasDatabaseName("ix_collection_tickets__run");

        // Shadow FKs so SaveChanges orders inserts.
        builder.HasOne<CollectionRun>()
            .WithMany()
            .HasForeignKey(x => x.CollectionRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.SignedOffByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
