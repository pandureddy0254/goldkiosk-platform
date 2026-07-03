namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>
/// Maps onto <c>customer.wallet_ledger_entries</c>. Append-only — does NOT
/// implement <c>IAuditableEntity</c>. New rows MUST be created via the
/// <c>customer.post_ledger_entry(...)</c> PL/pgSQL function, not direct INSERT.
/// </summary>
public sealed class WalletLedgerEntry
{
    /// <summary>Gets or sets the sequence no.</summary>
    public long SequenceNo { get; set; }
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the wallet id.</summary>
    public Guid WalletId { get; set; }
    /// <summary>Gets or sets the entry kind.</summary>
    public string EntryKind { get; set; } = string.Empty;
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
