using System.ComponentModel.DataAnnotations;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real;
using GoldKiosk.Kiosk.Devices.Registry;
using GoldKiosk.Kiosk.Devices.Simulation;

namespace GoldKiosk.Kiosk.Api.Devices;

/// <summary>
/// Composes the full device set once at startup: for every key in
/// <see cref="DeviceKeys.All"/> the simulated or real implementation is constructed per
/// the resolved <see cref="DeviceMode"/> (ADR 0004). Switching modes requires a restart;
/// no code path branches on "is mock" outside this composition root.
/// </summary>
/// <remarks>
/// <para>Real drivers receive their connection facts from
/// <see cref="DevicesOptions.ResolveConnection"/> (per-device override merged onto the
/// legacy-parity <see cref="RealDeviceDefaults"/>) and their tuning options bound from
/// <c>Devices:Overrides:{deviceKey}:{Section}</c> — e.g. <c>Devices:Overrides:scale:Scale</c>,
/// <c>Devices:Overrides:tray:Tray</c> — when an <see cref="IConfiguration"/> is supplied.
/// The tuning classes carry the legacy defaults in code, so configuration only needs the
/// overridden keys; like <see cref="DeviceOverride.Connection"/>, tuning sections are ignored
/// while a device runs in <see cref="DeviceMode.Mock"/>.</para>
/// <para>Driver variants are selected from the resolved connection's
/// <see cref="ConnectionOptions.Variant"/>: metal analyser <c>vanta</c> | <c>innovx</c>,
/// ID scanner <c>acuant</c> | <c>gemalto</c>. The tray and bagger take their dependency
/// device (sensor board / robotic arm) by port interface, so a real tray or bagger composes
/// against whichever implementation — real or simulated — the registry holds.</para>
/// </remarks>
internal static class DeviceComposition
{
    /// <summary>Builds the registry from the resolved per-device modes.</summary>
    /// <param name="options">The bound <c>Devices</c> options.</param>
    /// <param name="timeProvider">Time source handed to the simulators and real drivers.</param>
    /// <param name="configuration">
    /// Optional configuration root used to bind per-device tuning sections
    /// (<c>Devices:Overrides:{deviceKey}:{Section}</c>) for real drivers;
    /// <see langword="null"/> composes every real driver with its legacy-parity code defaults.
    /// </param>
    /// <returns>The immutable device registry.</returns>
    /// <exception cref="InvalidOperationException">
    /// An override references an unknown device key, a variant is unknown, or a bound tuning
    /// section fails data-annotation validation.
    /// </exception>
    public static DeviceRegistry CreateRegistry(
        DevicesOptions options,
        TimeProvider timeProvider,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        ValidateOverrideKeys(options);

        // Two-phase composition in dependency order: every self-contained device first, then
        // the two devices that consume another device's port (the tray verifies travel via
        // the sensor board; the bagger drives the arm).
        Dictionary<string, IKioskDevice> byKey = new(DeviceKeys.All.Count, StringComparer.Ordinal);
        foreach (string key in DeviceKeys.All)
        {
            if (key is not (DeviceKeys.Tray or DeviceKeys.Bagger))
            {
                byKey[key] = Create(key, options.ResolveMode(key), options, configuration, timeProvider);
            }
        }

        byKey[DeviceKeys.Tray] = CreateTray(
            options, configuration, timeProvider, (ISensorBoard)byKey[DeviceKeys.SensorBoard]);
        byKey[DeviceKeys.Bagger] = CreateBagger(
            options, timeProvider, (IRoboticArm)byKey[DeviceKeys.RoboticArm]);

        return new DeviceRegistry([.. DeviceKeys.All.Select(key => byKey[key])]);
    }

    private static void ValidateOverrideKeys(DevicesOptions options)
    {
        HashSet<string> known = [.. DeviceKeys.All.Select(Normalize)];
        List<string> unknown = [.. options.Overrides.Keys.Where(k => !known.Contains(Normalize(k)))];
        if (unknown.Count > 0)
        {
            throw new InvalidOperationException(
                $"Devices:Overrides contains unknown device keys: {string.Join(", ", unknown)}.");
        }
    }

    private static IKioskDevice Create(
        string key,
        DeviceMode mode,
        DevicesOptions options,
        IConfiguration? configuration,
        TimeProvider timeProvider) => (key, mode) switch
        {
            (DeviceKeys.Scale, DeviceMode.Mock) => new SimulatedScale(options.Simulation, timeProvider),
            (DeviceKeys.Scale, DeviceMode.Real) => new RealScale(
                options.ResolveConnection(DeviceKeys.Scale),
                BindTuning<ScaleOptions>(configuration, DeviceKeys.Scale, "Scale"),
                timeProvider),
            (DeviceKeys.MetalAnalyser, DeviceMode.Mock) => new SimulatedMetalAnalyser(options.Simulation, timeProvider),
            (DeviceKeys.MetalAnalyser, DeviceMode.Real) => CreateMetalAnalyser(options, configuration, timeProvider),
            (DeviceKeys.RoboticArm, DeviceMode.Mock) => new SimulatedRoboticArm(options.Simulation, timeProvider),
            (DeviceKeys.RoboticArm, DeviceMode.Real) => new RealRoboticArm(
                options.ResolveConnection(DeviceKeys.RoboticArm),
                BindTuning<RoboticArmOptions>(configuration, DeviceKeys.RoboticArm, "RoboticArm"),
                timeProvider),
            (DeviceKeys.VolumeChamber, DeviceMode.Mock) => new SimulatedVolumeChamber(options.Simulation, timeProvider),
            (DeviceKeys.VolumeChamber, DeviceMode.Real) => new RealVolumeChamber(
                options.ResolveConnection(DeviceKeys.VolumeChamber),
                BindTuning<VolumeChamberOptions>(configuration, DeviceKeys.VolumeChamber, "VolumeChamber"),
                timeProvider),
            (DeviceKeys.SensorBoard, DeviceMode.Mock) => new SimulatedSensorBoard(options.Simulation, timeProvider),
            (DeviceKeys.SensorBoard, DeviceMode.Real) => new RealSensorBoard(
                options.ResolveConnection(DeviceKeys.SensorBoard), timeProvider),
            (DeviceKeys.PowerRelays, DeviceMode.Mock) => new SimulatedPowerRelays(options.Simulation, timeProvider),
            (DeviceKeys.PowerRelays, DeviceMode.Real) => new RealPowerRelays(
                options.ResolveConnection(DeviceKeys.PowerRelays), timeProvider),
            (DeviceKeys.Camera, DeviceMode.Mock) => new SimulatedCameraService(options.Simulation, timeProvider),
            (DeviceKeys.Camera, DeviceMode.Real) => new RealCameraService(
                options.ResolveConnection(DeviceKeys.Camera),
                BindTuning<CameraServiceOptions>(configuration, DeviceKeys.Camera, "Camera"),
                timeProvider),
            (DeviceKeys.IdScanner, DeviceMode.Mock) => new SimulatedIdScanner(options.Simulation, timeProvider),
            (DeviceKeys.IdScanner, DeviceMode.Real) => CreateIdScanner(options, configuration, timeProvider),
            (DeviceKeys.FingerprintScanner, DeviceMode.Mock) => new SimulatedFingerprintScanner(options.Simulation, timeProvider),
            (DeviceKeys.FingerprintScanner, DeviceMode.Real) => new RealFingerprintScanner(
                options.ResolveConnection(DeviceKeys.FingerprintScanner),
                BindTuning<FingerprintScannerOptions>(configuration, DeviceKeys.FingerprintScanner, "FingerprintScanner"),
                timeProvider),
            (DeviceKeys.CashDispenser, DeviceMode.Mock) => new SimulatedCashDispenser(options.Simulation, timeProvider),
            (DeviceKeys.CashDispenser, DeviceMode.Real) => new RealCashDispenser(
                options.ResolveConnection(DeviceKeys.CashDispenser),
                BindTuning<CashDispenserOptions>(configuration, DeviceKeys.CashDispenser, "CashDispenser"),
                timeProvider),
            (DeviceKeys.LabelPrinter, DeviceMode.Mock) => new SimulatedLabelPrinter(options.Simulation, timeProvider),
            (DeviceKeys.LabelPrinter, DeviceMode.Real) => new RealLabelPrinter(
                BindTuning<LabelPrinterOptions>(configuration, DeviceKeys.LabelPrinter, "LabelPrinter"),
                timeProvider),
            _ => throw new InvalidOperationException($"No implementation registered for device key '{key}'."),
        };

    /// <summary>Selects the XRF driver for the fitted gun from the connection's variant.</summary>
    private static IKioskDevice CreateMetalAnalyser(
        DevicesOptions options,
        IConfiguration? configuration,
        TimeProvider timeProvider)
    {
        ConnectionOptions connection = options.ResolveConnection(DeviceKeys.MetalAnalyser);
        MetalAnalyserOptions tuning =
            BindTuning<MetalAnalyserOptions>(configuration, DeviceKeys.MetalAnalyser, "MetalAnalyser");

        if (IsVariant(connection, RealDeviceDefaults.AnalyserVariantVanta))
        {
            return new RealMetalAnalyser(connection, tuning, timeProvider);
        }

        if (IsVariant(connection, RealDeviceDefaults.AnalyserVariantInnovX))
        {
            return new RealMetalAnalyserInnovX(tuning, timeProvider);
        }

        throw new InvalidOperationException(
            $"Unknown metal analyser variant '{connection.Variant}' — expected " +
            $"'{RealDeviceDefaults.AnalyserVariantVanta}' or '{RealDeviceDefaults.AnalyserVariantInnovX}'.");
    }

    /// <summary>
    /// Validates the ID scanner variant and builds the driver; the Acuant/Gemalto split is
    /// resolved inside <see cref="RealIdScanner"/> from the same connection.
    /// </summary>
    private static RealIdScanner CreateIdScanner(
        DevicesOptions options,
        IConfiguration? configuration,
        TimeProvider timeProvider)
    {
        ConnectionOptions connection = options.ResolveConnection(DeviceKeys.IdScanner);
        if (!IsVariant(connection, RealDeviceDefaults.IdScannerVariantAcuant)
            && !IsVariant(connection, RealDeviceDefaults.IdScannerVariantGemalto))
        {
            throw new InvalidOperationException(
                $"Unknown ID scanner variant '{connection.Variant}' — expected " +
                $"'{RealDeviceDefaults.IdScannerVariantAcuant}' or '{RealDeviceDefaults.IdScannerVariantGemalto}'.");
        }

        return new RealIdScanner(
            connection,
            BindTuning<IdScannerOptions>(configuration, DeviceKeys.IdScanner, "IdScanner"),
            timeProvider);
    }

    /// <summary>Builds the tray; the real driver verifies travel via the composed sensor board.</summary>
    private static IKioskDevice CreateTray(
        DevicesOptions options,
        IConfiguration? configuration,
        TimeProvider timeProvider,
        ISensorBoard sensorBoard) => options.ResolveMode(DeviceKeys.Tray) switch
        {
            DeviceMode.Mock => new SimulatedTray(options.Simulation, timeProvider),
            DeviceMode.Real => new RealTray(
                sensorBoard,
                options.ResolveConnection(DeviceKeys.Tray),
                BindTuning<TrayOptions>(configuration, DeviceKeys.Tray, "Tray"),
                timeProvider),
            _ => throw new InvalidOperationException($"Unsupported device mode for '{DeviceKeys.Tray}'."),
        };

    /// <summary>Builds the bagger; the real driver delegates its choreography to the composed arm.</summary>
    private static IKioskDevice CreateBagger(
        DevicesOptions options,
        TimeProvider timeProvider,
        IRoboticArm roboticArm) => options.ResolveMode(DeviceKeys.Bagger) switch
        {
            DeviceMode.Mock => new SimulatedBagger(options.Simulation, timeProvider),
            DeviceMode.Real => new RealBagger(roboticArm, timeProvider),
            _ => throw new InvalidOperationException($"Unsupported device mode for '{DeviceKeys.Bagger}'."),
        };

    /// <summary>
    /// Builds a per-device tuning options instance: legacy-parity code defaults, with the
    /// device's <c>Devices:Overrides:{deviceKey}:{sectionName}</c> section bound on top when
    /// present, then data-annotation validated so an invalid kiosk fails fast at composition.
    /// </summary>
    private static T BindTuning<T>(IConfiguration? configuration, string deviceKey, string sectionName)
        where T : class, new()
    {
        var tuning = new T();
        IConfigurationSection? overrideSection =
            configuration is null ? null : FindOverrideSection(configuration, deviceKey);
        IConfigurationSection? section = overrideSection?.GetSection(sectionName);
        if (section is null || !section.Exists())
        {
            return tuning;
        }

        section.Bind(tuning);

        List<ValidationResult> failures = [];
        if (!Validator.TryValidateObject(tuning, new ValidationContext(tuning), failures, validateAllProperties: true))
        {
            string details = string.Join("; ", failures.Select(static f => f.ErrorMessage));
            throw new InvalidOperationException(
                $"Invalid device tuning at 'Devices:Overrides:{deviceKey}:{sectionName}': {details}");
        }

        return tuning;
    }

    /// <summary>
    /// Finds the override section for a device key using the same case- and
    /// underscore-insensitive matching as <see cref="DevicesOptions.ResolveMode"/>.
    /// </summary>
    private static IConfigurationSection? FindOverrideSection(IConfiguration configuration, string deviceKey)
    {
        string normalizedKey = Normalize(deviceKey);
        foreach (IConfigurationSection child in
            configuration.GetSection($"{DevicesOptions.SectionName}:Overrides").GetChildren())
        {
            if (Normalize(child.Key) == normalizedKey)
            {
                return child;
            }
        }

        return null;
    }

    private static bool IsVariant(ConnectionOptions connection, string variant) =>
        string.Equals(connection.Variant, variant, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string key) =>
        key.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
