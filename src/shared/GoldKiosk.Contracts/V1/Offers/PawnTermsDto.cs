using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Offers;

/// <summary>
/// Repayment terms for a pawn offer (shown in the explainer sheet, not the main screen).
/// </summary>
/// <param name="MonthlyFee">The monthly fee.</param>
/// <param name="AprPercent">The annualized percentage rate, e.g. <c>60.0</c>.</param>
/// <param name="TotalRepayment">The total amount due to redeem the item.</param>
/// <param name="DueDate">The redemption due date.</param>
public sealed record PawnTermsDto(
    MoneyDto MonthlyFee,
    decimal AprPercent,
    MoneyDto TotalRepayment,
    DateOnly DueDate);
