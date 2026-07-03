using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Kiosk;

/// <summary>Kiosk language configuration.</summary>
public sealed class KioskLanguageConfiguration : IEntityTypeConfiguration<KioskLanguage>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<KioskLanguage> builder)
    {
        builder.ToTable("kiosk_languages", "kiosk");
        builder.HasKey(kl => kl.Id);

        builder.HasOne(kl => kl.Language)
         .WithMany()
         .HasForeignKey(kl => kl.LanguageId)
         .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(kl => kl.KioskId).HasDatabaseName("ix_kiosk_languages__kiosk");
        builder.HasIndex(kl => new { kl.KioskId, kl.LanguageId })
         .HasDatabaseName("uq_kiosk_languages__kiosk_lang").IsUnique();
    }
}
