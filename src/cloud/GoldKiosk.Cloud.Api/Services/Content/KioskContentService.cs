using System.Text.Json;
using GoldKiosk.Contracts.V1.Cloud.Content;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.Api.Services.Content;

/// <summary>
/// EF-backed <see cref="IKioskContentService"/>. Tenant-specific rows win; global
/// defaults (<c>tenant_id IS NULL</c>) are the fallback — note the RLS caveat from the
/// parity analysis (§9 fix 3): the equality policy hides NULL-tenant rows from
/// non-BYPASSRLS roles; the backend role reads them, and the policy fix is tracked with
/// the schema work.
/// </summary>
/// <param name="db">The platform database.</param>
public sealed class KioskContentService(AppDbContext db) : IKioskContentService
{
    private static readonly JsonSerializerOptions TermsJsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync(
        Guid kioskId, CancellationToken cancellationToken = default)
    {
        List<LanguageDto> kioskLanguages = await db.KioskLanguages
            .AsNoTracking()
            .Where(kl => kl.KioskId == kioskId && kl.IsActive)
            .OrderBy(kl => kl.DisplayOrder)
            .Select(kl => new LanguageDto(
                kl.Language.Id,
                kl.Language.Code,
                kl.Language.NativeName,
                kl.Language.EnglishName,
                kl.DisplayOrder))
            .ToListAsync(cancellationToken);

        if (kioskLanguages.Count > 0)
        {
            return kioskLanguages;
        }

        // Fallback: English only (kiosk has no language mapping configured).
        return await db.Languages
            .AsNoTracking()
            .Where(l => l.Code == "en" && l.IsActive)
            .Select(l => new LanguageDto(l.Id, l.Code, l.NativeName, l.EnglishName, 1))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScreenSaverDto>> GetScreenSaversAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        List<ScreenSaverDto> slides = await QueryScreenSavers(tenantId)
            .ToListAsync(cancellationToken);
        if (slides.Count > 0)
        {
            return slides;
        }

        return await QueryScreenSavers(tenantId: null).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TermsResponse?> GetTermsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        Infrastructure.Entities.Kiosk.TenantTerms? row = await db.TenantTerms
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.IsActive)
                .OrderByDescending(t => t.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db.TenantTerms
                .AsNoTracking()
                .Where(t => t.TenantId == null && t.IsActive)
                .OrderByDescending(t => t.EffectiveDate)
                .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        List<TermsSectionDto> sections =
            JsonSerializer.Deserialize<List<TermsSectionDto>>(row.SectionsJson, TermsJsonOptions) ?? [];
        return new TermsResponse(row.Version, row.Title, row.EffectiveDate, sections);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemCategoryDto>> GetCategoriesAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        List<ItemCategoryDto> rows = await QueryCategories(tenantId)
            .ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            return rows;
        }

        return await QueryCategories(tenantId: null).ToListAsync(cancellationToken);
    }

    private IQueryable<ScreenSaverDto> QueryScreenSavers(Guid? tenantId) =>
        db.ScreenSavers
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ScreenSaverDto(
                s.Code, s.ImageUrl, s.MediaType, s.DurationSeconds, s.DisplayOrder));

    private IQueryable<ItemCategoryDto> QueryCategories(Guid? tenantId) =>
        db.ItemCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ItemCategoryDto(
                c.CategoryKey, c.DisplayName, c.Icon, c.AiFormClass,
                c.MinItems, c.MaxItems, c.DisplayOrder));
}
