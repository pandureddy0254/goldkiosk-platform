using System.Text.Json;

namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// A batched UI telemetry event. The UI never posts per selection — events accumulate and
/// ship with the tray commands (and their equivalents on the return path).
/// </summary>
/// <param name="At">When the event occurred on the kiosk.</param>
/// <param name="Event">The event name, e.g. <c>attract_engaged</c>, <c>service_selected</c>.</param>
/// <param name="Data">Optional event-specific payload, passed through opaquely.</param>
public sealed record ClientEventDto(DateTimeOffset At, string Event, JsonElement? Data);
