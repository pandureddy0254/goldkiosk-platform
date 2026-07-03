using GoldKiosk.Contracts.V1.Cloud.Content;

namespace GoldKiosk.Cloud.Api.Services.Content;

/// <summary>
/// Tenant-scoped reference content served to kiosks: languages, attract-loop slides,
/// terms &amp; conditions, and item categories (adapted from platform2's four data
/// services, now running behind the RLS interceptor with the app-level tenant filters
/// kept as defense-in-depth).
/// </summary>
public interface IKioskContentService
{
    /// <summary>Gets the languages configured for a kiosk (English fallback when unmapped).</summary>
    /// <param name="kioskId">The kiosk id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The ordered language list.</returns>
    Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync(
        Guid kioskId, CancellationToken cancellationToken = default);

    /// <summary>Gets the tenant's attract-loop slides (global defaults when none).</summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The ordered slide list.</returns>
    Task<IReadOnlyList<ScreenSaverDto>> GetScreenSaversAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets the tenant's active terms document (global default when none).</summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The terms document, or <see langword="null"/> when none is published.</returns>
    Task<TermsResponse?> GetTermsAsync(
        Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets the tenant's item categories (global defaults when none).</summary>
    /// <param name="tenantId">The tenant id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The ordered category list.</returns>
    Task<IReadOnlyList<ItemCategoryDto>> GetCategoriesAsync(
        Guid tenantId, CancellationToken cancellationToken = default);
}
