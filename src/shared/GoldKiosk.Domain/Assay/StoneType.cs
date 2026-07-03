namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The kind of set stone estimated within a jewellery item's outline. Distinguishes the density
/// and cut characteristics used by the (report-only) stone-weight model.
/// </summary>
public enum StoneType
{
    /// <summary>Unknown or unclassified stone.</summary>
    Unknown,

    /// <summary>A diamond, or a stone modelled with diamond-equivalent characteristics.</summary>
    DiamondEquivalent,

    /// <summary>Cubic zirconia.</summary>
    CubicZirconia,

    /// <summary>Ruby.</summary>
    Ruby,

    /// <summary>Sapphire.</summary>
    Sapphire,

    /// <summary>Glass or paste.</summary>
    Glass,
}
