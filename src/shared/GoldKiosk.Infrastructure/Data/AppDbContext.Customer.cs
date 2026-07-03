using GoldKiosk.Infrastructure.Entities.Customer;
using GoldKiosk.Infrastructure.Entities.Tx;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Infrastructure.Data;

/// <summary>
/// DbSets for the Customer and Tx aggregates. Kept partial so that the
/// canonical <c>AppDbContext.cs</c> stays focused on identity/kiosk wiring.
/// </summary>
public partial class AppDbContext
{
    // ─── Customer schema ────────────────────────────────────────────────────
    /// <summary>Set.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Set.</summary>
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    /// <summary>Set.</summary>
    public DbSet<CustomerIdentifier> CustomerIdentifiers => Set<CustomerIdentifier>();
    /// <summary>Set.</summary>
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    /// <summary>Set.</summary>
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    /// <summary>Set.</summary>
    public DbSet<CustomerWallet> CustomerWallets => Set<CustomerWallet>();
    /// <summary>Set.</summary>
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();

    // ─── Tx + payment.payouts ───────────────────────────────────────────────
    /// <summary>Set.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();
    /// <summary>Set.</summary>
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
    /// <summary>Set.</summary>
    public DbSet<Payout> Payouts => Set<Payout>();
}
