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
public sealed class IdempotencyTests : KioskApiTestBase
{
    [Test]
    public async Task TrayOpen_WithoutIdempotencyKeyHeader_Returns400()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);

        HttpResponseMessage response = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), idempotencyKey: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        JsonElement problem = await KioskApi.ReadJsonAsync(response);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.IdempotencyKeyConflict);
    }

    [Test]
    public async Task TrayOpen_ReplayedWithTheSameKey_ReturnsTheIdenticalAcceptedBody()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);
        string key = Guid.NewGuid().ToString();
        HttpResponseMessage original = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), key);
        string originalBody = await original.Content.ReadAsStringAsync();

        HttpResponseMessage replay = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), key);

        original.StatusCode.Should().Be(HttpStatusCode.Accepted);
        replay.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await replay.Content.ReadAsStringAsync()).Should().Be(
            originalBody, "a replayed key must return the original response, not re-execute the command");
    }

    [Test]
    public async Task TrayOpen_Replayed_DoesNotOpenTheTrayTwice()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);
        string key = Guid.NewGuid().ToString();
        await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), key);

        await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), key);

        SessionSnapshotResponse snapshot = await KioskApi.GetSnapshotAsync(Client, begun.SessionId);
        snapshot.State.Should().Be(SessionStates.PlacingItem, "the replay must not re-drive the state machine");
    }

    [Test]
    public async Task SameKeyOnADifferentOperation_Returns409KeyConflict()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);
        string key = Guid.NewGuid().ToString();
        await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), key);

        HttpResponseMessage crossOperation = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/close"), RequestMother.TrayClose(hasItem: true), key);

        crossOperation.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await KioskApi.ReadJsonAsync(crossOperation);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.IdempotencyKeyConflict);
        problem.GetProperty("session_id").GetString().Should().Be(begun.SessionId);
    }

    [Test]
    public async Task Settle_ReplayedWithTheSameKey_DoesNotDoubleSettle()
    {
        string sessionId = await KioskApi.DriveToPayoutAsync(Client);
        await KioskApi.PostExpectingAsync(
            Client, KioskApi.SessionPath(sessionId, "/payout"), RequestMother.CashPayout(), HttpStatusCode.Accepted);
        string key = Guid.NewGuid().ToString();
        HttpResponseMessage original = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(sessionId, "/settle"), new { }, key);
        string originalBody = await original.Content.ReadAsStringAsync();

        HttpResponseMessage replay = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(sessionId, "/settle"), new { }, key);

        original.StatusCode.Should().Be(HttpStatusCode.Accepted);
        replay.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await replay.Content.ReadAsStringAsync()).Should().Be(originalBody);
        SessionSnapshotResponse done = await KioskApi.PollSnapshotUntilAsync(
            Client, sessionId, s => s.State == SessionStates.Done, "settlement completion");
        done.State.Should().Be(SessionStates.Done);
    }
}
