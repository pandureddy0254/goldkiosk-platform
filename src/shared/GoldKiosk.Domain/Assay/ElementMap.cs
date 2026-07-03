using System.Globalization;

namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A lookup of element densities keyed by chemical symbol (case-insensitive), used by the
/// karat/assay math. Ported from the legacy <c>GCElemetMapProvider</c>; a real
/// <c>ElementMap.txt</c> file can override the embedded defaults via <see cref="Parse"/>.
/// </summary>
public sealed class ElementMap
{
    private readonly IReadOnlyDictionary<string, ElementInfo> _bySymbol;

    /// <summary>Creates a map from a dictionary of element rows keyed by symbol.</summary>
    /// <param name="bySymbol">
    /// The element rows keyed by symbol. The dictionary should compare keys case-insensitively
    /// (for example, using <see cref="StringComparer.OrdinalIgnoreCase"/>).
    /// </param>
    public ElementMap(IReadOnlyDictionary<string, ElementInfo> bySymbol)
    {
        ArgumentNullException.ThrowIfNull(bySymbol);
        _bySymbol = bySymbol;
    }

    /// <summary>Gets the number of elements in the map.</summary>
    public int Count => _bySymbol.Count;

    /// <summary>Gets all element rows in the map.</summary>
    public IReadOnlyCollection<ElementInfo> Elements =>
        _bySymbol.Values as IReadOnlyCollection<ElementInfo> ?? _bySymbol.Values.ToList();

    /// <summary>Determines whether the map contains the given element symbol (case-insensitive).</summary>
    /// <param name="symbol">The chemical symbol to test.</param>
    /// <returns><see langword="true"/> if the symbol is present; otherwise <see langword="false"/>.</returns>
    public bool Contains(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return _bySymbol.ContainsKey(symbol.Trim());
    }

    /// <summary>Gets the element row for the given symbol (case-insensitive).</summary>
    /// <param name="symbol">The chemical symbol to look up.</param>
    /// <returns>The element row.</returns>
    /// <exception cref="KeyNotFoundException">The symbol is not present in the map.</exception>
    public ElementInfo Get(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return _bySymbol.TryGetValue(symbol.Trim(), out var info)
            ? info
            : throw new KeyNotFoundException($"Element symbol '{symbol}' is not present in the element map.");
    }

    /// <summary>Gets the density (g/cm³) for the given element symbol (case-insensitive).</summary>
    /// <param name="symbol">The chemical symbol to look up.</param>
    /// <returns>The element's density in grams per cubic centimetre.</returns>
    /// <exception cref="KeyNotFoundException">The symbol is not present in the map.</exception>
    public decimal GetDensity(string symbol) => Get(symbol).Density;

    /// <summary>
    /// Parses the legacy <c>ElementMap.txt</c> format: one element per line, four
    /// <c>;</c>-delimited fields <c>Density;Name;Symbol;AtomicNumber</c>. Blank lines are
    /// ignored; symbol matching is case-insensitive.
    /// </summary>
    /// <param name="content">The full text of an element map file.</param>
    /// <returns>The parsed element map.</returns>
    /// <exception cref="FormatException">A row does not have exactly four fields, a numeric field cannot be parsed, a symbol is duplicated, or the content contains no rows.</exception>
    public static ElementMap Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var bySymbol = new Dictionary<string, ElementInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split(';');
            if (parts.Length != 4)
            {
                throw new FormatException(
                    $"Element map line must have four ';'-delimited fields (Density;Name;Symbol;AtomicNumber): '{line}'.");
            }

            if (!decimal.TryParse(parts[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var density))
            {
                throw new FormatException($"Element map density '{parts[0]}' is not a valid number in line '{line}'.");
            }

            if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var atomicNumber))
            {
                throw new FormatException($"Element map atomic number '{parts[3]}' is not a valid integer in line '{line}'.");
            }

            var symbol = parts[2].Trim();
            var info = new ElementInfo(density, parts[1].Trim(), symbol, atomicNumber);
            if (!bySymbol.TryAdd(symbol, info))
            {
                throw new FormatException($"Duplicate element symbol '{symbol}' in element map.");
            }
        }

        if (bySymbol.Count == 0)
        {
            throw new FormatException("Element map contained no element rows.");
        }

        return new ElementMap(bySymbol);
    }
}
