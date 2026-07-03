using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Services.Content;
using GoldKiosk.Contracts.V1.Cloud.Content;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Tenant-scoped reference content for kiosks: languages, attract-loop slides, terms and
/// item categories. Tenant identity comes from the JWT claim; the RLS interceptor scopes
/// every query.
/// </summary>
public static class ContentEndpoints
{
    /// <summary>Maps the content endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapContentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/languages", GetLanguagesAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/screensavers", GetScreenSaversAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/terms", GetTermsAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/categories", GetCategoriesAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<IReadOnlyList<LanguageDto>>, ProblemHttpResult>> GetLanguagesAsync(
        CurrentKioskService kiosk,
        IKioskContentService content,
        CancellationToken cancellationToken)
    {
        if (kiosk.KioskId is not Guid kioskId)
        {
            return Problems.KioskNotFound();
        }

        return TypedResults.Ok(await content.GetLanguagesAsync(kioskId, cancellationToken));
    }

    private static async Task<Results<Ok<IReadOnlyList<ScreenSaverDto>>, ProblemHttpResult>> GetScreenSaversAsync(
        CurrentKioskService kiosk,
        IKioskContentService content,
        CancellationToken cancellationToken)
    {
        if (kiosk.TenantId is not Guid tenantId)
        {
            return Problems.KioskNotFound();
        }

        return TypedResults.Ok(await content.GetScreenSaversAsync(tenantId, cancellationToken));
    }

    private static async Task<Results<Ok<TermsResponse>, ProblemHttpResult>> GetTermsAsync(
        CurrentKioskService kiosk,
        IKioskContentService content,
        CancellationToken cancellationToken)
    {
        if (kiosk.TenantId is not Guid tenantId)
        {
            return Problems.KioskNotFound();
        }

        TermsResponse? terms = await content.GetTermsAsync(tenantId, cancellationToken);
        return terms is null ? Problems.TermsNotFound() : TypedResults.Ok(terms);
    }

    private static async Task<Results<Ok<IReadOnlyList<ItemCategoryDto>>, ProblemHttpResult>> GetCategoriesAsync(
        CurrentKioskService kiosk,
        IKioskContentService content,
        CancellationToken cancellationToken)
    {
        if (kiosk.TenantId is not Guid tenantId)
        {
            return Problems.KioskNotFound();
        }

        return TypedResults.Ok(await content.GetCategoriesAsync(tenantId, cancellationToken));
    }
}
