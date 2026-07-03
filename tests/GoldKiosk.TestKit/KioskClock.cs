using Microsoft.Extensions.Time.Testing;

namespace GoldKiosk.TestKit;

/// <summary>
/// Factory for the deterministic <see cref="FakeTimeProvider"/> used across the suite.
/// All fixtures share one canonical start instant so expected folder names, offer
/// expiries and due dates are stable literals.
/// </summary>
public static class KioskClock
{
    /// <summary>The canonical test instant: 2026-07-02T14:00:00Z.</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a fake time provider pinned to <see cref="DefaultNow"/> (local zone UTC).</summary>
    public static FakeTimeProvider CreateTimeProvider() => new(DefaultNow);

    /// <summary>Creates a fake time provider pinned to the given instant (local zone UTC).</summary>
    public static FakeTimeProvider CreateTimeProvider(DateTimeOffset startAt) => new(startAt);
}
