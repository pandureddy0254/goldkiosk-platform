using FluentValidation;
using GoldKiosk.Contracts.V1.Tray;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>Validates the clubbed tray-close payload.</summary>
public sealed class TrayCloseRequestValidator : AbstractValidator<TrayCloseRequest>
{
    /// <summary>Initializes the rules.</summary>
    public TrayCloseRequestValidator()
    {
        RuleFor(x => x.Command).Equal("tray_close");
    }
}
