using FluentValidation;
using GoldKiosk.Contracts.V1.Cloud.Transactions;

namespace GoldKiosk.Cloud.Api.Validation;

/// <summary>
/// Validates <see cref="RecordTransactionRequest"/> — ids present, amounts positive,
/// measured facts within physical bounds.
/// </summary>
public sealed class RecordTransactionRequestValidator : AbstractValidator<RecordTransactionRequest>
{
    private static readonly string[] Kinds = ["sale", "pawn"];

    /// <summary>Initializes a new instance of the <see cref="RecordTransactionRequestValidator"/> class.</summary>
    public RecordTransactionRequestValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(k => Kinds.Contains(k, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Kind must be one of: sale, pawn.");
        RuleFor(x => x.Metal).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Karat).InclusiveBetween(1m, 24m);
        RuleFor(x => x.WeightGrams).InclusiveBetween(0.001m, 10_000m);
        RuleFor(x => x.PurityPercent).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Amount).NotNull();
        RuleFor(x => x.Amount.AmountMinor).GreaterThan(0).When(x => x.Amount is not null);
        RuleFor(x => x.Amount.Currency)
            .NotEmpty()
            .Length(3)
            .When(x => x.Amount is not null);
    }
}
