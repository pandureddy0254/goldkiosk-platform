using FluentValidation;
using GoldKiosk.Contracts.V1.Cloud.Auth;

namespace GoldKiosk.Cloud.Api.Validation;

/// <summary>
/// Validates <see cref="KioskLoginRequest"/> — both fields present, sane lengths.
/// </summary>
public sealed class KioskLoginRequestValidator : AbstractValidator<KioskLoginRequest>
{
    /// <summary>Initializes a new instance of the <see cref="KioskLoginRequestValidator"/> class.</summary>
    public KioskLoginRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Pin).NotEmpty().MaximumLength(128);
    }
}
