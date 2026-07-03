using FluentValidation;
using GoldKiosk.Contracts.V1.Sessions;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>Validates <see cref="AbortSessionRequest"/> at the boundary.</summary>
public sealed class AbortSessionRequestValidator : AbstractValidator<AbortSessionRequest>
{
    private static readonly string[] _reasons = ["timeout", "user_cancel", "operator", "fault"];

    /// <summary>Initializes the rules.</summary>
    public AbortSessionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(v => _reasons.Contains(v, StringComparer.Ordinal))
            .WithMessage("reason must be one of: timeout, user_cancel, operator, fault.");
    }
}
