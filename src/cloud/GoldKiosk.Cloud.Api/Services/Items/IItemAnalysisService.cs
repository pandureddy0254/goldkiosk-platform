using GoldKiosk.Cloud.Api.Services.Ai;
using GoldKiosk.Contracts.V1.Cloud.Items;

namespace GoldKiosk.Cloud.Api.Services.Items;

/// <summary>
/// Runs AI item verification and maps the raw verdict onto the kiosk-facing decision
/// per the binding AI item-verification design (degrade-open on transport failure only;
/// rejected/low-confidence verdicts escalate — never silently approved).
/// </summary>
public interface IItemAnalysisService
{
    /// <summary>Analyzes a tray image against the customer's selected category.</summary>
    /// <param name="image">The tray image.</param>
    /// <param name="category">The selected item category, e.g. <c>ring</c>.</param>
    /// <param name="transactionType">The transaction type, e.g. <c>sell</c> or <c>pawn</c>.</param>
    /// <param name="metalType">The expected metal, e.g. <c>gold</c>.</param>
    /// <param name="detailsByUserJson">Optional user-declared details as a JSON string.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The kiosk-facing verdict.</returns>
    Task<AnalyzeItemResponse> AnalyzeAsync(
        ItemImageUpload image,
        string category,
        string transactionType,
        string metalType,
        string? detailsByUserJson,
        CancellationToken cancellationToken = default);
}
