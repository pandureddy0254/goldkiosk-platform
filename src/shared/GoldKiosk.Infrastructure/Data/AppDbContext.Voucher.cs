using GoldKiosk.Infrastructure.Entities.Voucher;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Voucher-schema DbSets. Declared on the partial <see cref="AppDbContext"/>
/// so each domain module owns its own surface area.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    /// <summary>Set.</summary>
    public DbSet<RedemptionPolicy> RedemptionPolicies => Set<RedemptionPolicy>();
    /// <summary>Set.</summary>
    public DbSet<VoucherRedemption> VoucherRedemptions => Set<VoucherRedemption>();
    /// <summary>Set.</summary>
    public DbSet<PromotionalOffer> PromotionalOffers => Set<PromotionalOffer>();
}
