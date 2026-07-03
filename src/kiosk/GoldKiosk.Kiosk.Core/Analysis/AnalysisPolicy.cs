using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Options;

namespace GoldKiosk.Kiosk.Core.Analysis;

/// <summary>
/// Item acceptance rules applied over the pipeline measurements: weight gates, purity and
/// plating gates, and the legacy density/volume fraud cross-check. Failures carry the
/// stable rejection reason codes from <see cref="RejectionReasonCodes"/>.
/// </summary>
/// <param name="options">The configured acceptance thresholds.</param>
public sealed class AnalysisPolicy(AnalysisOptions options)
{
    private static readonly Dictionary<string, decimal> _densityGramsPerCc = new(StringComparer.Ordinal)
    {
        ["Au"] = 19.32m,
        ["Ag"] = 10.49m,
        ["Cu"] = 8.96m,
        ["Zn"] = 7.14m,
        ["Ni"] = 8.90m,
        ["Pt"] = 21.45m,
        ["Pd"] = 12.02m,
    };

    private const decimal FallbackDensityGramsPerCc = 8.0m;

    private readonly AnalysisOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gates the scale reading: zero weight is an empty tray; below the configured minimum
    /// is underweight.
    /// </summary>
    /// <param name="weightGrams">The stable weight reading in grams.</param>
    /// <returns>Success, or the rejection reason as a failure.</returns>
    public Result CheckWeight(decimal weightGrams)
    {
        if (weightGrams <= 0m)
        {
            return Result.Failure(new DomainError(
                RejectionReasonCodes.ItemEmptyTray, "The tray closed with nothing detectable on it."));
        }

        return weightGrams < _options.MinWeightGrams
            ? Result.Failure(new DomainError(
                RejectionReasonCodes.ItemUnderweight,
                $"The item weighs less than the accepted minimum of {_options.MinWeightGrams} g."))
            : Result.Success();
    }

    /// <summary>
    /// Gates the XRF composition: plated items and items below the configured minimum gold
    /// content are rejected; an unreadable composition is unidentified.
    /// </summary>
    /// <param name="goldPercent">Gold content in percent, when the analyser detected it.</param>
    /// <param name="goldPlated">Whether the analyser flagged surface plating.</param>
    /// <returns>Success, or the rejection reason as a failure.</returns>
    public Result CheckComposition(decimal? goldPercent, bool goldPlated)
    {
        if (goldPlated)
        {
            return Result.Failure(new DomainError(
                RejectionReasonCodes.ItemGoldPlated, "The item is gold plated, not solid precious metal."));
        }

        if (goldPercent is null)
        {
            return Result.Failure(new DomainError(
                RejectionReasonCodes.ItemUnidentified, "The item's composition could not be identified."));
        }

        return goldPercent < _options.MinGoldPercent
            ? Result.Failure(new DomainError(
                RejectionReasonCodes.ItemInsufficientPurity,
                $"The measured purity is below the accepted minimum of {_options.MinGoldPercent}%."))
            : Result.Success();
    }

    /// <summary>
    /// The fraud cross-check: compares the chamber-measured volume against the volume
    /// calculated from weight and elemental densities; deviation beyond the configured
    /// tolerance rejects the item as unidentified. Uncalibrated readings are advisory and pass.
    /// </summary>
    /// <param name="weightGrams">The item weight in grams.</param>
    /// <param name="elements">Elemental composition, symbol → percent.</param>
    /// <param name="measuredVolumeCc">The chamber-measured volume in cubic centimetres.</param>
    /// <param name="calibrated">Whether the chamber constants are current.</param>
    /// <returns>Success, or the rejection reason as a failure.</returns>
    public Result CheckVolume(
        decimal weightGrams,
        IReadOnlyDictionary<string, decimal> elements,
        decimal measuredVolumeCc,
        bool calibrated)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (!calibrated || weightGrams <= 0m)
        {
            return Result.Success();
        }

        decimal weightedDensity = 0m;
        decimal totalPercent = 0m;
        foreach ((string symbol, decimal percent) in elements)
        {
            decimal density = _densityGramsPerCc.GetValueOrDefault(symbol, FallbackDensityGramsPerCc);
            weightedDensity += density * percent / 100m;
            totalPercent += percent;
        }

        if (totalPercent <= 0m || weightedDensity <= 0m)
        {
            return Result.Success();
        }

        decimal calculatedVolumeCc = weightGrams / weightedDensity;
        if (calculatedVolumeCc <= 0m)
        {
            return Result.Success();
        }

        decimal deviationPercent = Math.Abs(measuredVolumeCc - calculatedVolumeCc) / calculatedVolumeCc * 100m;
        return deviationPercent > _options.VolumeTolerancePercent
            ? Result.Failure(new DomainError(
                RejectionReasonCodes.ItemUnidentified,
                "The measured volume does not match the analysed composition."))
            : Result.Success();
    }
}
