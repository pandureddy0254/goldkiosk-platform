using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedTrayContractTests : TrayContract
{
    protected override ITray CreateTray() => new SimulatedTray(Simulation, TimeProvider);
}
