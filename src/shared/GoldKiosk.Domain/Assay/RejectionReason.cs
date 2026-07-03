namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A machine-readable reason an assayed item is rejected, ported from the legacy
/// <c>RejectionReason</c> enum. Map to the stable wire codes via
/// <see cref="RejectionReasonCodeMap.ToCode"/>.
/// </summary>
public enum RejectionReason
{
    /// <summary>Tungsten content exceeds the rejection threshold (a common gold-bar counterfeit filler).</summary>
    ContainsTungsten,

    /// <summary>Platinum content exceeds the rejection threshold.</summary>
    ContainsPlatinum,

    /// <summary>Iridium content exceeds the rejection threshold.</summary>
    ContainsIridium,

    /// <summary>Rhodium content exceeds the rejection threshold.</summary>
    ContainsRhodium,

    /// <summary>Ruthenium content exceeds the rejection threshold.</summary>
    ContainsRuthenium,

    /// <summary>Palladium content exceeds the rejection threshold.</summary>
    ContainsPalladium,

    /// <summary>Lead content exceeds the rejection threshold.</summary>
    ContainsLead,

    /// <summary>Molybdenum content exceeds the rejection threshold.</summary>
    ContainsMolybdenum,

    /// <summary>Bismuth content exceeds the rejection threshold.</summary>
    ContainsBismuth,

    /// <summary>Cadmium content exceeds the rejection threshold.</summary>
    ContainsCadmium,

    /// <summary>Iron content exceeds the rejection threshold.</summary>
    ContainsIron,

    /// <summary>Manganese content exceeds the rejection threshold.</summary>
    ContainsManganese,

    /// <summary>Indium content exceeds the rejection threshold.</summary>
    ContainsIndium,

    /// <summary>The item is gold plated rather than solid precious metal.</summary>
    GoldPlated,

    /// <summary>The offer karat is below the minimum acceptable gold karat.</summary>
    LessThanAcceptableGoldKarat,

    /// <summary>The silver concentration is below the minimum acceptable silver percentage.</summary>
    LessThanAcceptableSilverPercent,

    /// <summary>The measured volume is inconsistent with the calculated volume (fraud cross-check).</summary>
    UnacceptableVolumeError,
}
