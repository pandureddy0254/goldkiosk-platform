namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Field values printed on a transaction bag label.</summary>
/// <param name="InvoiceNumber">Invoice/transaction reference number.</param>
/// <param name="BagNumber">Physical bag identifier.</param>
/// <param name="WeightGrams">Item weight in grams as recorded at analysis.</param>
/// <param name="OfferDisplay">The accepted offer, pre-formatted for display (amount + currency).</param>
public sealed record BagLabel(
    string InvoiceNumber,
    string BagNumber,
    decimal WeightGrams,
    string OfferDisplay);
