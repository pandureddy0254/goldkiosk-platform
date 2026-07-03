using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
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
internal static class DeviceComposition
{
    /// <summary>Builds the registry from the resolved per-device modes.</summary>
    /// <param name="options">The bound <c>Devices</c> options.</param>
    /// <param name="timeProvider">Time source handed to the simulators.</param>
    /// <returns>The immutable device registry.</returns>
    /// <exception cref="InvalidOperationException">An override references an unknown device key.</exception>
    public static DeviceRegistry CreateRegistry(DevicesOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        ValidateOverrideKeys(options);

        List<IKioskDevice> devices = [];
        foreach (string key in DeviceKeys.All)
        {
            devices.Add(Create(key, options.ResolveMode(key), options.Simulation, timeProvider));
        }

        return new DeviceRegistry(devices);
    }

    private static void ValidateOverrideKeys(DevicesOptions options)
    {
        static string Normalize(string key) =>
            key.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

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
        SimulationOptions simulation,
        TimeProvider timeProvider) => (key, mode) switch
        {
            (DeviceKeys.Scale, DeviceMode.Mock) => new SimulatedScale(simulation, timeProvider),
            (DeviceKeys.Scale, DeviceMode.Real) => new RealScale(),
            (DeviceKeys.MetalAnalyser, DeviceMode.Mock) => new SimulatedMetalAnalyser(simulation, timeProvider),
            (DeviceKeys.MetalAnalyser, DeviceMode.Real) => new RealMetalAnalyser(),
            (DeviceKeys.RoboticArm, DeviceMode.Mock) => new SimulatedRoboticArm(simulation, timeProvider),
            (DeviceKeys.RoboticArm, DeviceMode.Real) => new RealRoboticArm(),
            (DeviceKeys.VolumeChamber, DeviceMode.Mock) => new SimulatedVolumeChamber(simulation, timeProvider),
            (DeviceKeys.VolumeChamber, DeviceMode.Real) => new RealVolumeChamber(),
            (DeviceKeys.SensorBoard, DeviceMode.Mock) => new SimulatedSensorBoard(simulation, timeProvider),
            (DeviceKeys.SensorBoard, DeviceMode.Real) => new RealSensorBoard(),
            (DeviceKeys.PowerRelays, DeviceMode.Mock) => new SimulatedPowerRelays(simulation, timeProvider),
            (DeviceKeys.PowerRelays, DeviceMode.Real) => new RealPowerRelays(),
            (DeviceKeys.Camera, DeviceMode.Mock) => new SimulatedCameraService(simulation, timeProvider),
            (DeviceKeys.Camera, DeviceMode.Real) => new RealCameraService(),
            (DeviceKeys.IdScanner, DeviceMode.Mock) => new SimulatedIdScanner(simulation, timeProvider),
            (DeviceKeys.IdScanner, DeviceMode.Real) => new RealIdScanner(),
            (DeviceKeys.FingerprintScanner, DeviceMode.Mock) => new SimulatedFingerprintScanner(simulation, timeProvider),
            (DeviceKeys.FingerprintScanner, DeviceMode.Real) => new RealFingerprintScanner(),
            (DeviceKeys.CashDispenser, DeviceMode.Mock) => new SimulatedCashDispenser(simulation, timeProvider),
            (DeviceKeys.CashDispenser, DeviceMode.Real) => new RealCashDispenser(),
            (DeviceKeys.LabelPrinter, DeviceMode.Mock) => new SimulatedLabelPrinter(simulation, timeProvider),
            (DeviceKeys.LabelPrinter, DeviceMode.Real) => new RealLabelPrinter(),
            (DeviceKeys.Bagger, DeviceMode.Mock) => new SimulatedBagger(simulation, timeProvider),
            (DeviceKeys.Bagger, DeviceMode.Real) => new RealBagger(),
            (DeviceKeys.Tray, DeviceMode.Mock) => new SimulatedTray(simulation, timeProvider),
            (DeviceKeys.Tray, DeviceMode.Real) => new RealTray(),
            _ => throw new InvalidOperationException($"No implementation registered for device key '{key}'."),
        };
}
