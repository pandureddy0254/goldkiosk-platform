using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Pricing-schema DbSets. Declared on the partial <see cref="AppDbContext"/> so
/// each domain module owns its own surface area.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<MetalRateSource> MetalRateSources => Set<MetalRateSource>();
    /// <summary>Set.</summary>
    public DbSet<MetalRate> MetalRates => Set<MetalRate>();
    /// <summary>Set.</summary>
    public DbSet<Offer> Offers => Set<Offer>();
    /// <summary>Set.</summary>
    public DbSet<OfferLine> OfferLines => Set<OfferLine>();
}
