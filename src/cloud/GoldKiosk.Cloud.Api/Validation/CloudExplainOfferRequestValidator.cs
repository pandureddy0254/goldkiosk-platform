using FluentValidation;
using GoldKiosk.Contracts.V1.Cloud.Offers;

namespace GoldKiosk.Cloud.Api.Validation;

/// <summary>
/// Validates <see cref="CloudExplainOfferRequest"/> — a grounding reference must exist
/// and free-text fields stay bounded (data minimization: no PII belongs here).
/// </summary>
public sealed class CloudExplainOfferRequestValidator : AbstractValidator<CloudExplainOfferRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CloudExplainOfferRequestValidator"/> class.</summary>
    public CloudExplainOfferRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.OfferId is not null || !string.IsNullOrWhiteSpace(x.AmountDisplay))
            .WithMessage("Either offer_id or amount_display is required.");
        RuleFor(x => x.AmountDisplay).MaximumLength(64);
        RuleFor(x => x.ItemSummary).MaximumLength(128);
        RuleFor(x => x.Question).MaximumLength(500);
        RuleFor(x => x.Locale).MaximumLength(16);
    }
}
