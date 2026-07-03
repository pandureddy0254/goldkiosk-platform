using GoldKiosk.Infrastructure.Entities.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Master;

/// <summary>Language configuration.</summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages", "master");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).IsRequired();
        builder.Property(l => l.EnglishName).IsRequired();
        builder.Property(l => l.NativeName).IsRequired();
        builder.HasIndex(l => l.Code).HasDatabaseName("uq_languages__code").IsUnique();
    }
}
