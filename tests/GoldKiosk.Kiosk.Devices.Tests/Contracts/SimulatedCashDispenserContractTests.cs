using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedCashDispenserContractTests : CashDispenserContract
{
    protected override ICashDispenser CreateDispenser() => new SimulatedCashDispenser(Simulation, TimeProvider);
}
