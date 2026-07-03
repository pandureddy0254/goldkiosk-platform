namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>
/// Maps onto <c>kiosk.kiosk_languages</c>. Per-kiosk language ordering.
/// When a kiosk has no rows here the API falls back to <c>master.languages</c> default order.
/// </summary>
public sealed class KioskLanguage
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the language id.</summary>
    public Guid LanguageId { get; set; }
    /// <summary>Gets or sets the display order.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the language.</summary>
    public Master.Language Language { get; set; } = null!;
}
