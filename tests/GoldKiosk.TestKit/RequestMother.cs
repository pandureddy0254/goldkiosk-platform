using GoldKiosk.Contracts.V1.Contact;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Contracts.V1.Tray;

namespace GoldKiosk.TestKit;

/// <summary>
/// Object mothers for the Kiosk API wire requests, mirroring the payload shapes in
/// <c>docs/api/kiosk-api-payload-samples.md</c>.
/// </summary>
public static class RequestMother
{
    /// <summary>A valid-base64 one-pixel PNG for signature and image payloads.</summary>
    public const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    /// <summary>Begin-session request (payload samples §1).</summary>
    public static BeginSessionRequest BeginSession() =>
        new("en-US", "touch", KioskClock.DefaultNow);

    /// <summary>The clubbed tray-open request (payload samples §2).</summary>
    public static TrayOpenRequest TrayOpen(string serviceType = "sell") =>
        new("tray_open", SessionMother.Setup(serviceType), []);

    /// <summary>The clubbed tray-close request (payload samples §3).</summary>
    public static TrayCloseRequest TrayClose(bool hasItem) =>
        new("tray_close", hasItem, []);

    /// <summary>The offer-explain request (payload samples §5).</summary>
    public static ExplainOfferRequest ExplainOffer() =>
        new("Why is my offer lower than the gold price I saw online?");

    /// <summary>The clubbed signature request (payload samples §6).</summary>
    public static IdentitySignatureRequest Signature() =>
        new(OnePixelPngBase64, SessionMother.TermsVersion);

    /// <summary>Contact capture with email and phone (payload samples §7).</summary>
    public static ContactRequest Contact() => SessionMother.Contact();

    /// <summary>Contact capture selecting the QR channel only.</summary>
    public static ContactRequest QrOnlyContact() => new(null, null, ["qr"]);

    /// <summary>A cash payout request (payload samples §8).</summary>
    public static PayoutRequest CashPayout() => new("cash", null);

    /// <summary>A bank-transfer payout request with inline bank details (payload samples §8).</summary>
    public static PayoutRequest BankTransferPayout() => new("bank_transfer", SessionMother.BankDetails());

    /// <summary>An abort request (payload samples §10).</summary>
    public static AbortSessionRequest Abort(string reason = "user_cancel", bool returnItem = true) =>
        new(reason, returnItem);
}
