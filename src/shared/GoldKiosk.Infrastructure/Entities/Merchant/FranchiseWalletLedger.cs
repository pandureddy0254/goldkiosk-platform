namespace GoldKiosk.Infrastructure.Entities.Merchant;

/// <summary>
/// Maps onto <c>merchant.franchise_wallet_ledger</c>. Append-only — writes go through
/// the <c>merchant.post_franchise_ledger_entry</c> function, not direct EF inserts.
/// </summary>
public sealed class FranchiseWalletLedger
{
    /// <summary>Gets or sets the sequence no.</summary>
    public long SequenceNo { get; set; }
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the wallet id.</summary>
    public Guid WalletId { get; set; }

    /// <summary>Gets or sets the entry kind.</summary>
    public string EntryKind { get; set; } = string.Empty;        // topup | debit | adjustment | refund | commission
    /// <summary>Gets or sets the amount.</summary>
    public decimal Amount { get; set; }
    /// <summary>Gets or sets the balance after.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Gets or sets the reference type.</summary>
    public string? ReferenceType { get; set; }
    /// <summary>Gets or sets the reference id.</summary>
    public Guid? ReferenceId { get; set; }
    /// <summary>Gets or sets the occurred at.</summary>
    public DateTimeOffset OccurredAt { get; set; }
    /// <summary>Gets or sets the posted by user id.</summary>
    public Guid? PostedByUserId { get; set; }
}
