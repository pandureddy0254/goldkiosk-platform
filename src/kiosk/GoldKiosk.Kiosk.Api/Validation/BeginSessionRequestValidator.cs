using FluentValidation;
using GoldKiosk.Contracts.V1.Sessions;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>Validates <see cref="BeginSessionRequest"/> at the boundary.</summary>
public sealed class BeginSessionRequestValidator : AbstractValidator<BeginSessionRequest>
{
    /// <summary>Initializes the rules.</summary>
    public BeginSessionRequestValidator()
    {
        RuleFor(x => x.Locale)
            .NotEmpty()
            .Matches("^[a-z]{2,3}(-[A-Za-z]{2,4})?$");
        RuleFor(x => x.AttractSource)
            .NotEmpty()
            .MaximumLength(64);
    }
}
