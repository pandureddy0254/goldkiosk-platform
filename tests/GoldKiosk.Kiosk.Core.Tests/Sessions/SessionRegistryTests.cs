using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Sessions;

[TestFixture]
public sealed class SessionRegistryTests
{
    private SessionRegistry _registry = null!;

    [SetUp]
    public void CreateRegistry() => _registry = new SessionRegistry();

    [Test]
    public void Add_NewSession_MakesItResolvable()
    {
        TransactionSession session = SessionDriver.Begin();

        _registry.Add(session);

        _registry.TryGet(session.Id, out TransactionSession? found).Should().BeTrue();
        found.Should().BeSameAs(session);
    }

    [Test]
    public void Add_DuplicateSessionId_Throws()
    {
        TransactionSession session = SessionDriver.Begin();
        _registry.Add(session);

        _registry.Invoking(r => r.Add(session)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void TryGet_UnknownId_ReturnsFalse()
    {
        _registry.TryGet("ses_unknown", out TransactionSession? found).Should().BeFalse();
        found.Should().BeNull();
    }

    [Test]
    public void TryGet_BlankId_Throws()
    {
        _registry.Invoking(r => r.TryGet(" ", out _)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void ActiveSessions_ExcludesTerminalSessions()
    {
        TransactionSession active = SessionDriver.Begin();
        TransactionSession terminal = SessionDriver.InState(SessionStates.Done);
        _registry.Add(active);
        _registry.Add(terminal);

        _registry.ActiveSessions.Should().ContainSingle().Which.Should().BeSameAs(active);
    }
}
