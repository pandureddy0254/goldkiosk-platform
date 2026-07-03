namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// Spot prices returned by one market-feed call (per gram, deployment currency).
/// </summary>
/// <param name="Gold24kPerGram">Fine gold (24K) per gram.</param>
/// <param name="Gold22kPerGram">22K gold per gram.</param>
/// <param name="Gold18kPerGram">18K gold per gram.</param>
/// <param name="SilverPerGram">Fine silver per gram.</param>
public sealed record SpotRates(
    decimal Gold24kPerGram,
    decimal Gold22kPerGram,
    decimal Gold18kPerGram,
    decimal SilverPerGram);
