using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>
/// Maps onto <c>kiosk.kiosk_locations</c>. The physical site a kiosk lives in.
/// No audit columns on the underlying table.
/// </summary>
public sealed class KioskLocation : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid? StoreId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;        // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the address.</summary>
    public string? Address { get; set; }
    /// <summary>Gets or sets the latitude.</summary>
    public decimal? Latitude { get; set; }
    /// <summary>Gets or sets the longitude.</summary>
    public decimal? Longitude { get; set; }
    /// <summary>Gets or sets the region.</summary>
    public string? Region { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
