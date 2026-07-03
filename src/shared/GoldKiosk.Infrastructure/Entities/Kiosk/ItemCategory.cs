namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>Maps onto <c>kiosk.item_categories</c>. Null <see cref="TenantId"/> = default (all tenants).</summary>
public sealed class ItemCategory
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Gets or sets the category key.</summary>
    public string CategoryKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the icon.</summary>
    public string Icon { get; set; } = string.Empty;
    /// <summary>Gets or sets the ai form class.</summary>
    public string AiFormClass { get; set; } = "ANY";
    /// <summary>Gets or sets the min items.</summary>
    public int MinItems { get; set; } = 1;
    /// <summary>Gets or sets the max items.</summary>
    public int MaxItems { get; set; } = 1;
    /// <summary>Gets or sets the display order.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
