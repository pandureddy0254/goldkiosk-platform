namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.kiosk_inventory_snapshots</c>. Append-only snapshot of the
/// metal currently held by a given kiosk. Not directly tenant-scoped — the
/// tenancy is reached through <c>kiosk.kiosks</c>.
/// </summary>
public sealed class KioskInventorySnapshot
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the captured at.</summary>
    public DateTimeOffset CapturedAt { get; set; }
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = "gold";       // gold | silver | platinum | palladium
    /// <summary>Gets or sets the weight g.</summary>
    public decimal WeightG { get; set; }
    /// <summary>Gets or sets the carat.</summary>
    public decimal? Carat { get; set; }
}
