namespace GoldKiosk.Infrastructure.Entities.Master;

/// <summary>Maps onto <c>master.languages</c>. Global reference list of supported UI languages.</summary>
public sealed class Language
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;   // BCP-47, e.g. "en", "hi"
    /// <summary>Gets or sets the english name.</summary>
    public string EnglishName { get; set; } = string.Empty;
    /// <summary>Gets or sets the native name.</summary>
    public string NativeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the default order.</summary>
    public int DefaultOrder { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
