namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A single element reading from the XRF metal analyser: the element symbol, its measured
/// concentration as a mass percentage, and the analyser's reported measurement error.
/// </summary>
/// <param name="Symbol">The element's chemical symbol (case-insensitive, e.g. <c>Au</c>, <c>Ag</c>, <c>Cu</c>).</param>
/// <param name="Percent">The measured concentration as a mass percentage in the range 0–100.</param>
/// <param name="Error">The analyser's reported measurement error for this reading, as a percentage.</param>
public sealed record ElementReading(string Symbol, decimal Percent, decimal Error);
