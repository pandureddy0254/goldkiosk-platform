namespace GoldKiosk.Domain.Primitives;

/// <summary>
/// A stable, machine-readable domain failure carried by <see cref="Result"/>.
/// </summary>
/// <param name="Code">The stable machine-readable error code, e.g. <c>offer.expired</c>.</param>
/// <param name="Message">The human-readable description of the failure.</param>
public sealed record DomainError(string Code, string Message);
