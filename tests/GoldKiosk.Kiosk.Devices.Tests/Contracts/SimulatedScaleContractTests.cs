using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedScaleContractTests : ScaleContract
{
    protected override decimal ExpectedReferenceGrams => 12.4m;

    protected override IScale CreateScale() => new SimulatedScale(Simulation, TimeProvider);
}
