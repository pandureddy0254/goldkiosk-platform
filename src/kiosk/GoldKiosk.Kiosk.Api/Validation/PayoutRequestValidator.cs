using FluentValidation;
using GoldKiosk.Contracts.V1.Payout;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>
/// Validates the clubbed payout payload: a known method, and complete bank details when
/// the method is a bank transfer. Bank values are never logged.
/// </summary>
public sealed class PayoutRequestValidator : AbstractValidator<PayoutRequest>
{
    private static readonly string[] _methods = ["cash", "bank_transfer", "debit_card"];
    private static readonly string[] _accountTypes = ["checking", "savings"];

    /// <summary>Initializes the rules.</summary>
    public PayoutRequestValidator()
    {
        RuleFor(x => x.Method)
            .Must(v => _methods.Contains(v, StringComparer.Ordinal))
            .WithMessage("method must be one of: cash, bank_transfer, debit_card.");
        When(x => string.Equals(x.Method, "bank_transfer", StringComparison.Ordinal), () =>
        {
            RuleFor(x => x.Bank)
                .NotNull()
                .WithMessage("bank details are required for a bank transfer.");
            When(x => x.Bank is not null, () =>
            {
                // Null-forgiving is safe: these rules only run inside the Bank-not-null When().
                RuleFor(x => x.Bank!.AccountHolder).NotEmpty().MaximumLength(140);
                RuleFor(x => x.Bank!.RoutingNumber).Matches(@"^\d{9}$");
                RuleFor(x => x.Bank!.AccountNumber).Matches(@"^\d{4,17}$");
                RuleFor(x => x.Bank!.AccountType)
                    .Must(v => _accountTypes.Contains(v, StringComparer.Ordinal))
                    .WithMessage("account_type must be 'checking' or 'savings'.");
            });
        });
    }
}
