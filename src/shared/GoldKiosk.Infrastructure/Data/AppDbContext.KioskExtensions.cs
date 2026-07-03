using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Additional <c>kiosk</c>-schema DbSets owned by the Operations module
/// (location configuration + per-kiosk security tokens). Kept in a separate
/// partial file so the canonical <see cref="AppDbContext"/> declarations for
/// <c>kiosks</c> and <c>screen_savers</c> are not disturbed.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<KioskLocation> KioskLocations => Set<KioskLocation>();
    /// <summary>Set.</summary>
    public DbSet<KioskSecurityToken> KioskSecurityTokens => Set<KioskSecurityToken>();
}
