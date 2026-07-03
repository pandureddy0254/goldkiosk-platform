namespace GoldKiosk.Contracts.V1.Settlement;

/// <summary>
/// The terminal receipt delivered in the <c>session_completed</c> event and the snapshot.
/// </summary>
/// <param name="ReceiptId">The receipt identifier, e.g. <c>rcp_01JZC…</c>.</param>
/// <param name="InvoiceNumber">The human-readable invoice number, e.g. <c>USGK-000412</c>.</param>
/// <param name="QrPayload">The URL encoded into the on-screen QR code.</param>
/// <param name="ChannelsSent">The channels the receipt was delivered to, e.g. <c>qr</c>, <c>email</c>.</param>
public sealed record ReceiptDto(
    string ReceiptId,
    string InvoiceNumber,
    string QrPayload,
    IReadOnlyList<string> ChannelsSent);
