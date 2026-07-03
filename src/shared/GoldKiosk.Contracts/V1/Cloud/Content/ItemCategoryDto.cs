namespace GoldKiosk.Contracts.V1.Cloud.Content;

/// <summary>
/// An accepted item category served from <c>GET /api/v1/categories</c>.
/// </summary>
/// <param name="CategoryKey">The stable category key, e.g. <c>ring</c>.</param>
/// <param name="DisplayName">The customer-facing name.</param>
/// <param name="Icon">The icon asset key.</param>
/// <param name="AiFormClass">The AI form class used for item verification, e.g. <c>RING</c>.</param>
/// <param name="MinItems">The minimum item count per tray.</param>
/// <param name="MaxItems">The maximum item count per tray.</param>
/// <param name="DisplayOrder">The kiosk display order.</param>
public sealed record ItemCategoryDto(
    string CategoryKey,
    string DisplayName,
    string Icon,
    string AiFormClass,
    int MinItems,
    int MaxItems,
    int DisplayOrder);
