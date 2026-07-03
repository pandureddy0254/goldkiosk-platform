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
public sealed class SessionLifecycleTests : KioskApiTestBase
{
    [Test]
    public async Task CloseTray_WithoutItem_ReturnsTheSessionToWelcome()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);
        await KioskApi.PostExpectingAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), HttpStatusCode.Accepted);

        HttpResponseMessage close = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/tray/close"), RequestMother.TrayClose(hasItem: false));

        close.StatusCode.Should().Be(HttpStatusCode.Accepted);
        JsonElement body = await KioskApi.ReadJsonAsync(close);
        body.GetProperty("state").GetString().Should().Be(SessionStates.Welcome);
        body.GetProperty("tray").GetProperty("status").GetString().Should().Be("closing");
    }

    [Test]
    public async Task BeginSession_WhileAnotherSessionIsActive_Returns409InvalidState()
    {
        await KioskApi.BeginSessionAsync(Client);

        HttpResponseMessage second = await KioskApi.PostAsync(
            Client, "/api/v1/sessions", RequestMother.BeginSession(), null);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await KioskApi.ReadJsonAsync(second);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.SessionInvalidState);
    }

    [Test]
    public async Task GetSnapshot_UnknownSessionId_Returns404SessionNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync(KioskApi.SessionPath("ses_UNKNOWN"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        JsonElement problem = await KioskApi.ReadJsonAsync(response);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.SessionNotFound);
        problem.GetProperty("session_id").GetString().Should().Be("ses_UNKNOWN");
    }

    [Test]
    public async Task Abort_ActiveSessionWithoutItem_Returns202AndFinishesTheSession()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);

        HttpResponseMessage abort = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(begun.SessionId, "/abort"), RequestMother.Abort("user_cancel"));

        abort.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(abort)).GetProperty("state").GetString().Should().Be(SessionStates.Done);
    }

    [Test]
    public async Task BeginSession_AfterThePreviousSessionAborted_Succeeds()
    {
        BeginSessionResponse first = await KioskApi.BeginSessionAsync(Client);
        await KioskApi.PostExpectingAsync(
            Client, KioskApi.SessionPath(first.SessionId, "/abort"), RequestMother.Abort("user_cancel"), HttpStatusCode.Accepted);

        BeginSessionResponse second = await KioskApi.BeginSessionAsync(Client);

        second.SessionId.Should().NotBe(first.SessionId);
        second.State.Should().Be(SessionStates.Welcome);
    }
}
