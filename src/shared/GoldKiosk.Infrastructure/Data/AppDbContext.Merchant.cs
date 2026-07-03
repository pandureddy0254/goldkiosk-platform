using GoldKiosk.Infrastructure.Entities.Merchant;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// Merchant-schema DbSets. Declared on the partial <see cref="AppDbContext"/>
/// so each domain module owns its own surface area.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Set.</summary>
    public DbSet<Merchant> Merchants => Set<Merchant>();
    /// <summary>Set.</summary>
    public DbSet<MerchantBankAccount> MerchantBankAccounts => Set<MerchantBankAccount>();
    /// <summary>Set.</summary>
    public DbSet<FranchiseWallet> FranchiseWallets => Set<FranchiseWallet>();
    /// <summary>Set.</summary>
    public DbSet<WalletTopupRequest> WalletTopupRequests => Set<WalletTopupRequest>();
    /// <summary>Set.</summary>
    public DbSet<FranchiseWalletLedger> FranchiseWalletLedger => Set<FranchiseWalletLedger>();
}
