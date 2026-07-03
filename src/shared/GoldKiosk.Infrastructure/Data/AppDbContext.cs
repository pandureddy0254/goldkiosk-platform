using GoldKiosk.Infrastructure.Entities.Identity;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// The application's EF Core <see cref="DbContext"/>.
/// <para>
/// Inherits from <see cref="IdentityUserContext{TUser, TKey}"/> rather than
/// <c>IdentityDbContext</c> because we manage roles ourselves via <see cref="AppRole"/>
/// and <see cref="AppRolePermission"/> — we don't use Identity's claims-based role system.
/// </para>
/// <para>
/// The DB schema lives in <c>db/</c> and is the source of truth. EF Core does NOT
/// generate migrations against this context; entity configurations exist only so
/// that LINQ queries work against the right tables.
/// </para>
/// </summary>
public partial class AppDbContext : IdentityUserContext<AppUser, Guid>
{
    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── Identity schema (custom, not ASP.NET Identity defaults) ────────────
    /// <summary>Set.</summary>
    public DbSet<AppRole> Roles => Set<AppRole>();
    /// <summary>Set.</summary>
    public DbSet<AppPermission> Permissions => Set<AppPermission>();
    /// <summary>Set.</summary>
    public DbSet<AppRolePermission> RolePermissions => Set<AppRolePermission>();
    /// <summary>Set.</summary>
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    /// <summary>Set.</summary>
    public DbSet<AppUserMfa> UserMfas => Set<AppUserMfa>();
    /// <summary>Set.</summary>
    public DbSet<AppUserSession> UserSessions => Set<AppUserSession>();
    /// <summary>Set.</summary>
    public DbSet<AppModule> Modules => Set<AppModule>();

    // ─── Master schema (global reference tables) ────────────────────────────
    /// <summary>Set.</summary>
    public DbSet<Entities.Master.Language> Languages => Set<Entities.Master.Language>();

    // ─── Kiosk schema ───────────────────────────────────────────────────────
    /// <summary>Set.</summary>
    public DbSet<Entities.Kiosk.Kiosk> Kiosks => Set<Entities.Kiosk.Kiosk>();
    /// <summary>Set.</summary>
    public DbSet<Entities.Kiosk.ScreenSaver> ScreenSavers => Set<Entities.Kiosk.ScreenSaver>();
    /// <summary>Set.</summary>
    public DbSet<Entities.Kiosk.KioskLanguage> KioskLanguages => Set<Entities.Kiosk.KioskLanguage>();
    /// <summary>Set.</summary>
    public DbSet<Entities.Kiosk.TenantTerms> TenantTerms => Set<Entities.Kiosk.TenantTerms>();
    /// <summary>Set.</summary>
    public DbSet<Entities.Kiosk.ItemCategory> ItemCategories => Set<Entities.Kiosk.ItemCategory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply every IEntityTypeConfiguration in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Move Identity's auxiliary tables onto the `identity` schema and rename
        // them to the snake_case form created in db/0028.
        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("aspnet_user_claims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("aspnet_user_logins", "identity");
        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("aspnet_user_tokens", "identity");
    }
}
