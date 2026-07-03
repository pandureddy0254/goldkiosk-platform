using System.Globalization;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// One Dobot Cartesian pose parsed from a configuration waypoint string
/// (<c>x:y:z:r</c>; a trailing fifth legacy component is tolerated and ignored — the legacy
/// point table carried a fifth axis value the CR firmware never consumed).
/// </summary>
/// <param name="X">X coordinate in mm.</param>
/// <param name="Y">Y coordinate in mm.</param>
/// <param name="Z">Z coordinate in mm.</param>
/// <param name="R">Rotation in degrees.</param>
public sealed record ArmWaypoint(double X, double Y, double Z, double R)
{
    /// <summary>Parses a waypoint string with the invariant culture.</summary>
    /// <param name="value">The <c>x:y:z:r[:extra]</c> string.</param>
    /// <param name="name">The waypoint name, used in the failure message.</param>
    /// <returns>The parsed waypoint.</returns>
    /// <exception cref="FormatException">The string does not contain four numeric components.</exception>
    public static ArmWaypoint Parse(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 4
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
            || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
        {
            throw new FormatException($"arm_waypoint_invalid:{name}");
        }

        return new ArmWaypoint(x, y, z, r);
    }
}
