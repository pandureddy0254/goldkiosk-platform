namespace GoldKiosk.Contracts.V1.Contact;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/contact</c> — email, phone and receipt
/// channels clubbed into one call. Email/phone are optional when the only channel is <c>qr</c>.
/// </summary>
/// <param name="Email">The customer's email address, when provided.</param>
/// <param name="Phone">The customer's phone number in E.164 format, when provided.</param>
/// <param name="ReceiptChannels">The receipt channels, e.g. <c>qr</c>, <c>email</c>, <c>sms</c>. QR is always included.</param>
public sealed record ContactRequest(string? Email, string? Phone, IReadOnlyList<string> ReceiptChannels);
