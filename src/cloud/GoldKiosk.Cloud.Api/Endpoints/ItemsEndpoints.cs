using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Cloud.Api.Services.Ai;
using GoldKiosk.Cloud.Api.Services.Items;
using GoldKiosk.Contracts.V1.Cloud.Items;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// AI item verification (multipart tray images + selected category) via the in-house
/// goldkiosk-ai service. Tray images only — PII imagery never reaches this route.
/// </summary>
public static class ItemsEndpoints
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/bmp", "image/webp"];

    /// <summary>Maps the item-analysis endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapItemsEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/items/analyze", AnalyzeAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .DisableAntiforgery() // bearer-authenticated machine API; no cookies in play
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<AnalyzeItemResponse>, ProblemHttpResult, ValidationProblem>>
        AnalyzeAsync(
            [FromForm] string? category,
            [FromForm(Name = "transaction_type")] string? transactionType,
            [FromForm(Name = "metal_type")] string? metalType,
            [FromForm(Name = "details_by_user")] string? detailsByUser,
            IFormFileCollection images,
            IItemAnalysisService analysis,
            IOptions<AiServiceOptions> aiOptions,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["category"] = ["The category form field is required."],
            });
        }

        IFormFile? file = images.FirstOrDefault(f => f.Length > 0);
        if (file is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["images"] = ["At least one non-empty image is required."],
            });
        }

        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Problems.UnsupportedMediaType(file.ContentType);
        }

        long maxBytes = aiOptions.Value.MaxImageBytes;
        if (file.Length > maxBytes)
        {
            return Problems.UploadTooLarge(maxBytes);
        }

        ItemImageUpload upload = await ReadAsync(file, cancellationToken);
        AnalyzeItemResponse response = await analysis.AnalyzeAsync(
            upload,
            category,
            string.IsNullOrWhiteSpace(transactionType) ? "sell" : transactionType,
            string.IsNullOrWhiteSpace(metalType) ? "gold" : metalType,
            detailsByUser,
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<ItemImageUpload> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return new ItemImageUpload(buffer.ToArray(), file.FileName, file.ContentType);
    }
}
