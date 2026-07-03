namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A single detected stone region within a jewellery item's outline, described by its projected
/// area on the rectified top-down image.
/// </summary>
/// <param name="AreaMm2">The stone's projected area in square millimetres.</param>
public sealed record StoneRegion(double AreaMm2);
