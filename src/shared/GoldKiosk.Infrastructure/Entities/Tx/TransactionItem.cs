namespace GoldKiosk.Infrastructure.Entities.Tx;

/// <summary>Maps onto <c>tx.transaction_items</c>.</summary>
public sealed class TransactionItem
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the item index.</summary>
    public short ItemIndex { get; set; }
    /// <summary>Gets or sets the item type.</summary>
    public string ItemType { get; set; } = string.Empty;
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the declared karat.</summary>
    public decimal? DeclaredKarat { get; set; }
    /// <summary>Gets or sets the declared weight g.</summary>
    public decimal? DeclaredWeightG { get; set; }
    /// <summary>Gets or sets the billed weight g.</summary>
    public decimal BilledWeightG { get; set; }
    /// <summary>Gets or sets the billed karat.</summary>
    public decimal BilledKarat { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
