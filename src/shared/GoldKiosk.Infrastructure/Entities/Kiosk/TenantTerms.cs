namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>Maps onto <c>kiosk.tenant_terms</c>. T&amp;C content served to the KioskApp.
/// NULL TenantId = global default shown to any tenant with no custom terms.</summary>
public sealed class TenantTerms
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the effective date.</summary>
    public DateOnly EffectiveDate { get; set; }
    /// <summary>Gets or sets the sections json.</summary>
    public string SectionsJson { get; set; } = "[]";   // JSONB: [{heading, body}]
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
