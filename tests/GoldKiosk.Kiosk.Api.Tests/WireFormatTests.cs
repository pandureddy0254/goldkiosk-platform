using System.Net.Http.Json;
using FluentAssertions;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.Api.Tests.Support;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Api.Tests;

[TestFixture]
public sealed class WireFormatTests : KioskApiTestBase
{
    [Test]
    public async Task BeginSession_ResponseBody_IsSnakeCaseOnTheWire()
    {
        HttpResponseMessage response = await KioskApi.PostAsync(
            Client, "/api/v1/sessions", RequestMother.BeginSession(), null);

        string raw = await response.Content.ReadAsStringAsync();

        raw.Should().Contain("\"session_id\"");
        raw.Should().Contain("\"offer_ttl_seconds\"");
        raw.Should().Contain("\"idle_timeout_seconds\"");
        raw.Should().Contain("\"payout_methods\"");
        raw.Should().Contain("\"terms_version\"");
        raw.Should().NotContain("\"SessionId\"");
    }

    [Test]
    public async Task BeginSession_SnakeCaseBody_RoundTripsThroughTheSharedWireOptions()
    {
        HttpResponseMessage response = await KioskApi.PostAsync(
            Client, "/api/v1/sessions", RequestMother.BeginSession(), null);
        string raw = await response.Content.ReadAsStringAsync();

        BeginSessionResponse? typed = System.Text.Json.JsonSerializer.Deserialize<BeginSessionResponse>(
            raw, WireJson.Options);

        typed.Should().NotBeNull();
        typed!.SessionId.Should().StartWith("ses_");
        typed.Features.PayoutMethods.Should().Equal("cash", "bank_transfer", "debit_card");
    }

    [Test]
    public async Task BeginSession_SnakeCaseRequestBody_IsBoundByTheHost()
    {
        // RequestMother serializes through the same snake_case options the host uses; a
        // 201 with echoed defaults proves the request-side contract binds.
        HttpResponseMessage response = await KioskApi.PostAsync(
            Client, "/api/v1/sessions", RequestMother.BeginSession(), null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        BeginSessionResponse body =
            (await response.Content.ReadFromJsonAsync<BeginSessionResponse>(WireJson.Options))!;
        body.State.Should().Be("welcome");
        body.TermsVersion.Should().Be(SessionMother.TermsVersion);
    }

    [Test]
    public async Task GetSnapshot_OfferBody_UsesTheSnakeCaseMoneyEnvelope()
    {
        string sessionId = await KioskApi.DriveToOfferAsync(Client);

        string raw = await Client.GetStringAsync(KioskApi.SessionPath(sessionId));

        raw.Should().Contain("\"offer_id\"");
        raw.Should().Contain("\"amount_minor\"");
        raw.Should().Contain("\"expires_at\"");
        raw.Should().Contain("\"is_test\"");
    }
}
