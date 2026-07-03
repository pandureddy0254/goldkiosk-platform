using System.Net;
using System.Text.Json;
using FluentAssertions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.Api.Tests.Support;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Api.Tests;

/// <summary>
/// Cash feasibility gate. The mock rate table is switched to JPY (zero minor-unit digits):
/// the ¥865 offer becomes 865 minor units, which is not a whole number of the dispenser's
/// 100-minor-unit notes — so the cash bill mix is infeasible while the offer itself is valid.
/// </summary>
[TestFixture]
public sealed class PayoutInsufficientCashTests : KioskApiTestBase
{
    protected override IReadOnlyDictionary<string, string?> ConfigurationOverrides { get; } =
        new Dictionary<string, string?>
        {
            ["MockRates:Currency"] = "JPY",
        };

    [Test]
    public async Task ConfirmPayout_CashForANonWholeDollarOffer_Returns409WithRerouteMethods()
    {
        string sessionId = await KioskApi.DriveToPayoutAsync(Client);

        HttpResponseMessage payout = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/payout"), RequestMother.CashPayout());

        payout.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement problem = await KioskApi.ReadJsonAsync(payout);
        problem.GetProperty("type").GetString().Should().Be(ProblemTypes.PayoutInsufficientCash);
        problem.GetProperty("session_id").GetString().Should().Be(sessionId);
        problem.GetProperty("available_methods").EnumerateArray().Select(m => m.GetString())
            .Should().Equal("bank_transfer", "debit_card");
    }

    [Test]
    public async Task ConfirmPayout_CashRefused_LeavesTheSessionInPayoutForRerouting()
    {
        string sessionId = await KioskApi.DriveToPayoutAsync(Client);
        await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/payout"), RequestMother.CashPayout());

        SessionSnapshotResponse snapshot = await KioskApi.GetSnapshotAsync(Client, sessionId);

        snapshot.State.Should().Be(SessionStates.Payout, "the customer must still be able to pick another method");
        snapshot.Offer!.Amount.Currency.Should().Be("JPY");
        snapshot.Offer.Amount.AmountMinor.Should().Be(865);
    }

    [Test]
    public async Task ConfirmPayout_BankTransferAfterCashWasRefused_Succeeds()
    {
        string sessionId = await KioskApi.DriveToPayoutAsync(Client);
        await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/payout"), RequestMother.CashPayout());

        HttpResponseMessage reroute = await KioskApi.PostKeyedAsync(
            Client, KioskApi.SessionPath(sessionId, "/payout"), RequestMother.BankTransferPayout());

        reroute.StatusCode.Should().Be(HttpStatusCode.Accepted);
        JsonElement body = await KioskApi.ReadJsonAsync(reroute);
        body.GetProperty("state").GetString().Should().Be(SessionStates.PayoutConfirmed);
        body.GetProperty("payout").GetProperty("method").GetString().Should().Be("bank_transfer");
    }
}
