using GoldKiosk.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldKiosk.Infrastructure.Configurations.Identity;

/// <summary>App user configuration.</summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users", "identity");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        // Map IdentityUser core columns explicitly so we don't depend on naming convention quirks.
        builder.Property(u => u.UserName).HasColumnName("user_name").HasColumnType("citext");
        builder.Property(u => u.NormalizedUserName).HasColumnName("normalized_user_name").HasColumnType("citext");
        builder.Property(u => u.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        builder.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasColumnType("citext");
        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
        builder.Property(u => u.SecurityStamp).HasColumnName("security_stamp");
        builder.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        builder.Property(u => u.PhoneNumber).HasColumnName("mobile");
        builder.Property(u => u.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        builder.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
        builder.Property(u => u.LockoutEnabled).HasColumnName("lockout_enabled");
        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");

        // Our extra columns
        builder.Property(u => u.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(u => u.ExternalSubject).HasColumnName("external_subject");
        builder.Property(u => u.OtpMode).HasColumnName("otp_mode").IsRequired();
        builder.Property(u => u.Status).HasColumnName("status").IsRequired();
        builder.Property(u => u.Locale).HasColumnName("locale").IsRequired();
        builder.Property(u => u.PasswordResetRequired).HasColumnName("password_reset_required");
        builder.Property(u => u.LastSignedInAt).HasColumnName("last_signed_in_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        // Identity expects these indexes; the SQL already created them.
        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ux_users__normalized_email").IsUnique();
        builder.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ix_users__normalized_user_name");
    }
}
