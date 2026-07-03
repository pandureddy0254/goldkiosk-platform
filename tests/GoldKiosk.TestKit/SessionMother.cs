using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Contact;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Settlement;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.TestKit;

/// <summary>
/// Object mothers for the session-level DTOs and Kiosk.Core records, shaped after
/// <c>docs/api/kiosk-api-payload-samples.md</c>.
/// </summary>
public static class SessionMother
{
    /// <summary>The terms version used across the suite (payload samples §1).</summary>
    public const string TermsVersion = "2026-06-01.v3";

    /// <summary>Deterministic customer email used to assert PII handling.</summary>
    public const string Email = "customer@example.com";

    /// <summary>Deterministic customer phone used to assert PII handling.</summary>
    public const string Phone = "+13125550147";

    /// <summary>The clubbed pre-tray setup (payload samples §2).</summary>
    public static SetupDto Setup(string serviceType = "sell") =>
        new(serviceType, "en-US", new TermsAcceptanceDto(TermsVersion, Accepted: true, KioskClock.DefaultNow), null, null);

    /// <summary>An $865.00 sale offer expiring at the given instant.</summary>
    public static OfferDto Offer(DateTimeOffset expiresAt, string kind = "sale") =>
        new(
            "off_01TESTOFFER00000000000000",
            new MoneyDto(86500, "USD", "$865.00"),
            kind,
            Verified: true,
            LivePrice: true,
            expiresAt,
            PawnTerms: null);

    /// <summary>A rejection payload for the returning-item path.</summary>
    public static RejectionDto Rejection(string reasonCode = "item.underweight") =>
        new(reasonCode, "This item is below the minimum weight", "return_item");

    /// <summary>A terminal receipt.</summary>
    public static ReceiptDto Receipt() =>
        new("rcp_01TESTRECEIPT000000000000", "USGK-000001", "https://r.goldkiosk.com/t/rcp_TEST", ["qr"]);

    /// <summary>The clubbed contact capture with email and phone (payload samples §7).</summary>
    public static ContactRequest Contact() => new(Email, Phone, ["qr", "email"]);

    /// <summary>Customer facts as extracted from the ID scan.</summary>
    public static CustomerFacts Customer() => new("Jordan", "Avery", new DateOnly(1994, 3, 15));

    /// <summary>A confirmed cash payout with a planned bill mix.</summary>
    public static PayoutSelection CashPayout(IReadOnlyDictionary<int, int>? plannedBills = null) =>
        new("cash", Bank: null, plannedBills ?? new Dictionary<int, int> { [100] = 8, [50] = 1, [10] = 1, [5] = 1 }, BillMixOk: true);

    /// <summary>A confirmed bank-transfer payout carrying restricted bank details.</summary>
    public static PayoutSelection BankPayout() =>
        new("bank_transfer", BankDetails(), PlannedBills: null, BillMixOk: null);

    /// <summary>Bank details as posted from the payout screen (payload samples §8).</summary>
    public static BankDetailsDto BankDetails() =>
        new("Jordan Avery", "071000013", "000123456789", "checking");
}
