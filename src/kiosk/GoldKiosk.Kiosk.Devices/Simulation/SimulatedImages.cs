namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>Deterministic embedded image bytes shared by the simulated camera and ID scanner.</summary>
internal static class SimulatedImages
{
    /// <summary>A valid 1×1 transparent PNG (shared instance — treat as read-only).</summary>
    internal static byte[] OnePixelPng { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
}
