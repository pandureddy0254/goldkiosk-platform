using FluentValidation;
using GoldKiosk.Contracts.V1.Cloud.Offers;

namespace GoldKiosk.Cloud.Api.Validation;

/// <summary>
/// Validates <see cref="CloudOfferRequest"/> — measured item facts within physical bounds.
/// </summary>
public sealed class CloudOfferRequestValidator : AbstractValidator<CloudOfferRequest>
{
    private static readonly string[] Metals = ["gold", "silver"];
    private static readonly string[] Kinds = ["sale", "pawn"];

    /// <summary>Initializes a new instance of the <see cref="CloudOfferRequestValidator"/> class.</summary>
    public CloudOfferRequestValidator()
    {
        RuleFor(x => x.Metal)
            .NotEmpty()
            .Must(m => Metals.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Metal must be one of: gold, silver.");
        RuleFor(x => x.Karat).InclusiveBetween(1m, 24m);
        RuleFor(x => x.PurityPercent).InclusiveBetween(0.01m, 100m);
        RuleFor(x => x.WeightGrams).InclusiveBetween(0.001m, 10_000m);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(k => Kinds.Contains(k, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Kind must be one of: sale, pawn.");
    }
}
