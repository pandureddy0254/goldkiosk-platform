namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A row in the element density table: density plus identifying metadata, ported from the
/// legacy <c>ElementMap.txt</c> (format <c>Density;Name;Symbol;AtomicNumber</c>).
/// </summary>
/// <param name="Density">The element's density in grams per cubic centimetre (g/cm³).</param>
/// <param name="Name">The element's full name (e.g. <c>Gold</c>).</param>
/// <param name="Symbol">The element's chemical symbol (e.g. <c>Au</c>).</param>
/// <param name="AtomicNumber">The element's atomic number.</param>
public sealed record ElementInfo(decimal Density, string Name, string Symbol, int AtomicNumber);
