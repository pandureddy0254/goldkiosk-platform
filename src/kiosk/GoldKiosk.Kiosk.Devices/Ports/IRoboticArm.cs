using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Robotic arm port — all physical item movement inside the machine. Real target: Dobot over
/// TCP (dashboard :29999, motion :30003/:30004) plus the Kollmorgen AKD linear axis (telnet
/// ASCII), with motion-complete confirmation replacing the legacy fire-and-forget waits.
/// </summary>
public interface IRoboticArm : IKioskDevice
{
    /// <summary>Executes one named movement and returns when motion has completed.</summary>
    /// <param name="move">The movement to perform.</param>
    /// <param name="cancellationToken">Cancels waiting for motion completion (the arm still finishes safely).</param>
    /// <returns>A task that completes when the arm reports the motion finished.</returns>
    Task MoveAsync(ArmMove move, CancellationToken cancellationToken = default);
}
