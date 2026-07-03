using GoldKiosk.Infrastructure.Entities.Identity;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App user session configuration.</summary>
public sealed class AppUserSessionConfiguration : IEntityTypeConfiguration<AppUserSession>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppUserSession> builder)
    {
        builder.ToTable("user_sessions", "identity");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TokenHash).HasDatabaseName("uq_user_sessions__token").IsUnique();

        // Shadow FK so SaveChanges orders inserts (users → user_sessions).
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
