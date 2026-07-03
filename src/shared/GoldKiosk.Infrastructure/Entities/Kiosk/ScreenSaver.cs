namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>Maps onto <c>kiosk.screen_savers</c>. Idle-screen content per tenant.
/// NULL TenantId = global default shown to any tenant with no custom slides.</summary>
public sealed class ScreenSaver
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;            // citext
    /// <summary>Gets or sets the image URL.</summary>
    public string ImageUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the media type.</summary>
    public string MediaType { get; set; } = "image";            // image | video
    /// <summary>Gets or sets the duration seconds.</summary>
    public int? DurationSeconds { get; set; }
    /// <summary>Gets or sets the display order.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
