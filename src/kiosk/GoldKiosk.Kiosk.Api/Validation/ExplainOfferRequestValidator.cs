using FluentValidation;
using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>Validates <see cref="ExplainOfferRequest"/> (question is optional).</summary>
public sealed class ExplainOfferRequestValidator : AbstractValidator<ExplainOfferRequest>
{
    /// <summary>Initializes the rules.</summary>
    public ExplainOfferRequestValidator()
    {
        RuleFor(x => x.Question).MaximumLength(500);
    }
}
