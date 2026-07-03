using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedIdScannerContractTests : IdScannerContract
{
    protected override IIdScanner CreateScanner() => new SimulatedIdScanner(Simulation, TimeProvider);
}
