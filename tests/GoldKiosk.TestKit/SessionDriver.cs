using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.TestKit;

/// <summary>
/// Drives a <see cref="TransactionSession"/> to a named wire state through its own guarded
/// transitions (never by reflection), using the canonical mothers and
/// <see cref="KioskClock.DefaultNow"/>. A failed intermediate step throws — the driver is a
/// test guard, not an assertion.
/// </summary>
public static class SessionDriver
{
    /// <summary>The instant every driven transition executes at.</summary>
    public static readonly DateTimeOffset Now = KioskClock.DefaultNow;

    /// <summary>Begins a fresh test session in the <c>welcome</c> state.</summary>
    public static TransactionSession Begin(bool isTest = true) =>
        TransactionSession.Begin($"ses_{Guid.NewGuid():N}", isTest, Now);

    /// <summary>Builds a session sitting in the requested wire state.</summary>
    /// <param name="state">The target state from <see cref="SessionStates"/>.</param>
    /// <param name="serviceType">The service selected at tray open: <c>sell</c> or <c>pawn</c>.</param>
    /// <returns>The driven session.</returns>
    public static TransactionSession InState(string state, string serviceType = "sell")
    {
        TransactionSession session = Begin();
        switch (state)
        {
            case SessionStates.Welcome:
                break;
            case SessionStates.PlacingItem:
                Require(session.OpenTray(SessionMother.Setup(serviceType)));
                break;
            case SessionStates.Analyzing:
                DriveToAnalyzing(session, serviceType);
                break;
            case SessionStates.Offer:
                DriveToOffer(session, serviceType);
                break;
            case SessionStates.Identity:
                DriveToIdentity(session, serviceType);
                break;
            case SessionStates.Contact:
                DriveToContact(session, serviceType);
                break;
            case SessionStates.Payout:
                DriveToPayout(session, serviceType);
                break;
            case SessionStates.PayoutConfirmed:
                DriveToPayoutConfirmed(session, serviceType);
                break;
            case SessionStates.Settling:
                DriveToPayoutConfirmed(session, serviceType);
                Require(session.StartSettlement());
                break;
            case SessionStates.ReturningItem:
                DriveToAnalyzing(session, serviceType);
                Require(session.RejectItem(SessionMother.Rejection()));
                break;
            case SessionStates.Done:
                Require(session.Abort("user_cancel", returnItem: false));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown session state.");
        }

        return session;
    }

    /// <summary>
    /// Builds a session whose identity pipeline has legitimately reached the signature
    /// step: identity started, ID scanned (customer recorded) and the signature step in
    /// progress — the precondition for <see cref="TransactionSession.CompleteSignature"/>.
    /// </summary>
    public static TransactionSession AtSignatureStep(string serviceType = "sell")
    {
        TransactionSession session = InState(SessionStates.Identity, serviceType);
        Require(session.StartIdentity(fingerprintRequired: false));
        session.UpdateIdentityStep("id_scan", "completed");
        session.RecordCustomer(SessionMother.Customer());
        session.UpdateIdentityStep("face_match", "completed");
        session.UpdateIdentityStep("signature", "in_progress");
        return session;
    }

    private static void DriveToAnalyzing(TransactionSession session, string serviceType)
    {
        Require(session.OpenTray(SessionMother.Setup(serviceType)));
        Require(session.CloseTray(hasItem: true));
    }

    private static void DriveToOffer(TransactionSession session, string serviceType)
    {
        DriveToAnalyzing(session, serviceType);
        Require(session.PresentOffer(SessionMother.Offer(Now.AddMinutes(10)), Now));
    }

    private static void DriveToIdentity(TransactionSession session, string serviceType)
    {
        DriveToOffer(session, serviceType);
        Require(session.AcceptOffer(Now));
    }

    private static void DriveToContact(TransactionSession session, string serviceType)
    {
        DriveToOffer(session, serviceType);
        Require(session.AcceptOffer(Now));
        Require(session.StartIdentity(fingerprintRequired: false));
        session.UpdateIdentityStep("id_scan", "completed");
        session.RecordCustomer(SessionMother.Customer());
        session.UpdateIdentityStep("face_match", "completed");
        session.UpdateIdentityStep("signature", "in_progress");
        Require(session.CompleteSignature(SessionMother.TermsVersion));
    }

    private static void DriveToPayout(TransactionSession session, string serviceType)
    {
        DriveToContact(session, serviceType);
        Require(session.SetContact(SessionMother.Contact()));
    }

    private static void DriveToPayoutConfirmed(TransactionSession session, string serviceType)
    {
        DriveToPayout(session, serviceType);
        Require(session.ConfirmPayout(SessionMother.CashPayout()));
    }

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Session driver step failed: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
