using System.Net;
using System.Text.Json;
using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.Api.Tests.Support;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Api.Tests;

[TestFixture]
public sealed class SessionGuardTests : KioskApiTestBase
{
    [Test]
    public async Task SubmitSignature_StraightAfterOfferAccept_Returns409InvalidState()
    {
        string sessionId = await KioskApi.DriveToOfferAsync(Client);
        await KioskApi.PostExpectingAsync(
            Client, KioskApi.SessionPath(sessionId, "/offer/accept"), new { }, HttpStatusCode.Accepted);

        HttpResponseMessage bypass = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/identity/signature"), RequestMother.Signature());

        bypass.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await KioskApi.ReadJsonAsync(bypass);
        problem.GetProperty("type").GetString().Should().Be(
            ProblemTypes.SessionInvalidState,
            "a signature posted without walking the KYC gates must be refused");
        problem.GetProperty("session_id").GetString().Should().Be(sessionId);
    }

    [Test]
    public async Task Abort_AfterSettleWasAccepted_Returns409InvalidState()
    {
        string sessionId = await KioskApi.DriveToSettlingAsync(Client);

        HttpResponseMessage abort = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/abort"), RequestMother.Abort("user_cancel"));

        // Money is in motion: the abort is refused whether the session is still settling or
        // has just completed — both surface session.invalid_state.
        abort.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await KioskApi.ReadJsonAsync(abort);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.SessionInvalidState);
    }

    [Test]
    public async Task Abort_DuringSettlement_DoesNotStopTheSettlementFromCompleting()
    {
        string sessionId = await KioskApi.DriveToSettlingAsync(Client);
        await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/abort"), RequestMother.Abort("user_cancel"));

        SessionSnapshotResponse done = await KioskApi.PollSnapshotUntilAsync(
            Client, sessionId, s => s.State == SessionStates.Done, "settlement completion despite the abort attempt");

        done.State.Should().Be(SessionStates.Done);
        string journal = await Factory.WaitForJournalAsync(
            text => text.Contains("\"receipt\":", StringComparison.Ordinal),
            "the settled journal with a receipt");
        journal.Should().Contain("\"abort_reason\": null", "the refused abort must leave no trace on the record");
    }

    [Test]
    public async Task AcceptOffer_SecondTime_Returns409InvalidState()
    {
        string sessionId = await KioskApi.DriveToOfferAsync(Client);
        await KioskApi.PostExpectingAsync(
            Client, KioskApi.SessionPath(sessionId, "/offer/accept"), new { }, HttpStatusCode.Accepted);

        HttpResponseMessage second = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/offer/accept"), new { });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await KioskApi.ReadJsonAsync(second)).GetProperty("type").GetString()
            .Should().Be(ProblemTypes.SessionInvalidState);
    }

    [Test]
    public async Task Settle_BeforePayoutConfirmation_Returns409InvalidState()
    {
        string sessionId = await KioskApi.DriveToPayoutAsync(Client);

        HttpResponseMessage settle = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/settle"), new { });

        settle.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await KioskApi.ReadJsonAsync(settle)).GetProperty("type").GetString()
            .Should().Be(ProblemTypes.SessionInvalidState);
    }
}
