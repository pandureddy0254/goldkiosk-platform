using System.Buffers.Text;
using FluentValidation;
using GoldKiosk.Contracts.V1.Identity;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>
/// Validates the clubbed signature payload: a decodable base64 PNG and the terms version
/// it signs.
/// </summary>
public sealed class IdentitySignatureRequestValidator : AbstractValidator<IdentitySignatureRequest>
{
    /// <summary>Initializes the rules.</summary>
    public IdentitySignatureRequestValidator()
    {
        RuleFor(x => x.SignaturePngBase64)
            .NotEmpty()
            .Must(v => v is not null && Base64.IsValid(v))
            .WithMessage("signature_png_base64 must be valid base64.");
        RuleFor(x => x.SignedTermsVersion).NotEmpty();
    }
}
