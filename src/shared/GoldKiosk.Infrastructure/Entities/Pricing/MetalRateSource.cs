namespace GoldKiosk.Infrastructure.Entities.Pricing;

/// <summary>Maps onto <c>pricing.metal_rate_sources</c>.</summary>
public sealed class MetalRateSource
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;          // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the adapter type.</summary>
    public string AdapterType { get; set; } = string.Empty;
    /// <summary>Gets or sets the poll interval seconds.</summary>
    public int PollIntervalSeconds { get; set; } = 300;
    /// <summary>Gets or sets the freshness sla seconds.</summary>
    public int FreshnessSlaSeconds { get; set; } = 900;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
