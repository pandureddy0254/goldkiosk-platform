using FluentValidation;
using GoldKiosk.Contracts.V1.Tray;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>
/// Validates the clubbed tray-open payload: correct command discriminator, a chosen
/// service, and explicit terms acceptance before any hardware moves.
/// </summary>
public sealed class TrayOpenRequestValidator : AbstractValidator<TrayOpenRequest>
{
    private static readonly string[] _serviceTypes = ["sell", "pawn"];

    /// <summary>Initializes the rules.</summary>
    public TrayOpenRequestValidator()
    {
        RuleFor(x => x.Command).Equal("tray_open");
        RuleFor(x => x.Setup).NotNull();
        When(x => x.Setup is not null, () =>
        {
            RuleFor(x => x.Setup.ServiceType)
                .Must(v => _serviceTypes.Contains(v, StringComparer.Ordinal))
                .WithMessage("service_type must be 'sell' or 'pawn'.");
            RuleFor(x => x.Setup.Locale).NotEmpty();
            RuleFor(x => x.Setup.Terms).NotNull();
            When(x => x.Setup.Terms is not null, () =>
            {
                RuleFor(x => x.Setup.Terms.Version).NotEmpty();
                RuleFor(x => x.Setup.Terms.Accepted)
                    .Equal(true)
                    .WithMessage("Terms must be accepted before the tray opens.");
            });
        });
    }
}
