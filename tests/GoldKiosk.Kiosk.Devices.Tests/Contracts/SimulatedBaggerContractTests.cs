using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedBaggerContractTests : BaggerContract
{
    protected override IBagger CreateBagger() => new SimulatedBagger(Simulation, TimeProvider);
}
