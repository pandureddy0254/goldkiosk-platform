using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.Api.Tests.Support;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Api.Tests;

[TestFixture]
public sealed class HappyPathTests : KioskApiTestBase
{
    [Test]
    public async Task FullCashSaleFlow_WithSimulatedDevices_RunsWelcomeToDoneWithReceipt()
    {
        BeginSessionResponse begun = await KioskApi.BeginSessionAsync(Client);
        begun.SessionId.Should().StartWith("ses_");
        begun.State.Should().Be(SessionStates.Welcome);
        begun.Sequence.Should().Be(1);
        begun.Features.PayoutMethods.Should().Contain("cash");
        begun.OfferTtlSeconds.Should().Be(600);
        begun.TermsVersion.Should().Be(SessionMother.TermsVersion);
        string id = begun.SessionId;

        HttpResponseMessage open = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/tray/open"), RequestMother.TrayOpen());
        open.StatusCode.Should().Be(HttpStatusCode.Accepted);
        JsonElement openBody = await KioskApi.ReadJsonAsync(open);
        openBody.GetProperty("state").GetString().Should().Be(SessionStates.PlacingItem);
        openBody.GetProperty("tray").GetProperty("status").GetString().Should().Be("opening");

        HttpResponseMessage close = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/tray/close"), RequestMother.TrayClose(hasItem: true));
        close.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(close)).GetProperty("state").GetString().Should().Be(SessionStates.Analyzing);

        SessionSnapshotResponse atOffer = await KioskApi.PollSnapshotUntilAsync(
            Client, id, s => s.State == SessionStates.Offer, "the presented offer");
        atOffer.IsTest.Should().BeTrue("every device is simulated");
        atOffer.Offer.Should().NotBeNull();
        atOffer.Offer!.OfferId.Should().StartWith("off_");
        atOffer.Offer.Kind.Should().Be("sale");
        atOffer.Offer.Verified.Should().BeTrue();
        atOffer.Offer.Amount.Should().Be(new MoneyDto(86_500, "USD", "$865.00"));
        atOffer.Offer.ExpiresAt.Should().Be(KioskClock.DefaultNow.AddSeconds(600));

        HttpResponseMessage explain = await KioskApi.PostAsync(
            Client, KioskApi.SessionPath(id, "/offer/explain"), RequestMother.ExplainOffer(), null);
        explain.StatusCode.Should().Be(HttpStatusCode.OK);
        ExplainOfferResponse explanation =
            (await explain.Content.ReadFromJsonAsync<ExplainOfferResponse>(WireJson.Options))!;
        explanation.Explanation.Should().Contain("$865.00", "the explainer must ground itself in the actual offer");
        explanation.Disclaimer.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage accept = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/offer/accept"), new { });
        accept.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(accept)).GetProperty("state").GetString().Should().Be(SessionStates.Identity);

        HttpResponseMessage identityStart = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/identity/start"), new { });
        identityStart.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(identityStart))
            .GetProperty("identity").GetProperty("current_step").GetString().Should().Be("id_scan");

        SessionSnapshotResponse atSignature = await KioskApi.PollSnapshotUntilAsync(
            Client, id, s => s.Identity?.CurrentStep == "signature", "the signature step");
        atSignature.Identity!.Steps.Should().Contain(s => s.Step == "id_scan" && s.Status == "completed");
        atSignature.Identity.Steps.Should().Contain(s => s.Step == "face_match" && s.Status == "completed");

        HttpResponseMessage signature = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/identity/signature"), RequestMother.Signature());
        signature.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(signature)).GetProperty("state").GetString().Should().Be(SessionStates.Contact);

        HttpResponseMessage contact = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/contact"), RequestMother.Contact());
        contact.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(contact)).GetProperty("state").GetString().Should().Be(SessionStates.Payout);

        HttpResponseMessage payout = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/payout"), RequestMother.CashPayout());
        payout.StatusCode.Should().Be(HttpStatusCode.Accepted);
        JsonElement payoutBody = await KioskApi.ReadJsonAsync(payout);
        payoutBody.GetProperty("state").GetString().Should().Be(SessionStates.PayoutConfirmed);
        payoutBody.GetProperty("payout").GetProperty("method").GetString().Should().Be("cash");
        payoutBody.GetProperty("payout").GetProperty("bill_mix_ok").GetBoolean().Should().BeTrue();

        HttpResponseMessage settle = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(id, "/settle"), new { });
        settle.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await KioskApi.ReadJsonAsync(settle)).GetProperty("state").GetString().Should().Be(SessionStates.Settling);

        SessionSnapshotResponse done = await KioskApi.PollSnapshotUntilAsync(
            Client, id, s => s.State == SessionStates.Done, "settlement completion");
        done.IsTest.Should().BeTrue();

        string journal = await Factory.WaitForJournalAsync(
            text => text.Contains("\"receipt\":", StringComparison.Ordinal)
                && text.Contains("rcp_", StringComparison.Ordinal),
            "the settled journal with a receipt");
        journal.Should().Contain("\"invoice_number\": \"USGK-000001\"");
        journal.Should().Contain("\"abort_reason\": null");
        Factory.HasTransactionDetailsFile().Should().BeTrue("settlement writes the legacy transactionDetails.json");
    }
}
