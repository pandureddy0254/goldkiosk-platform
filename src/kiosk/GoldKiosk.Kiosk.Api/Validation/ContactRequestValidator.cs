using FluentValidation;
using GoldKiosk.Contracts.V1.Contact;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>
/// Validates the clubbed contact payload: at least the QR channel; email/phone required
/// only when their channel was selected.
/// </summary>
public sealed class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    private static readonly string[] _channels = ["qr", "email", "sms"];

    /// <summary>Initializes the rules.</summary>
    public ContactRequestValidator()
    {
        RuleFor(x => x.ReceiptChannels)
            .NotEmpty()
            .Must(v => v is not null && v.All(c => _channels.Contains(c, StringComparer.Ordinal)))
            .WithMessage("receipt_channels may contain only: qr, email, sms.");
        RuleFor(x => x.Email)
            .NotEmpty()
            .When(x => x.ReceiptChannels?.Contains("email", StringComparer.Ordinal) == true)
            .WithMessage("email is required when the email receipt channel is selected.");
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone)
            .NotEmpty()
            .When(x => x.ReceiptChannels?.Contains("sms", StringComparer.Ordinal) == true)
            .WithMessage("phone is required when the sms receipt channel is selected.");
        RuleFor(x => x.Phone)
            .Matches(@"^\+[1-9]\d{6,14}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("phone must be in E.164 format.");
    }
}
