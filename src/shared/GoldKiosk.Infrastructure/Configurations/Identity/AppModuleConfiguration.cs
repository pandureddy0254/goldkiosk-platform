using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App module configuration.</summary>
public sealed class AppModuleConfiguration : IEntityTypeConfiguration<AppModule>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppModule> builder)
    {
        builder.ToTable("modules", "identity");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired();
        builder.HasIndex(m => new { m.Controller, m.Action }).HasDatabaseName("uq_modules__controller_action").IsUnique();
    }
}
