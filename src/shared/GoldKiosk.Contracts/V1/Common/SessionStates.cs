namespace GoldKiosk.Contracts.V1.Common;

/// <summary>
/// Wire values for the kiosk session state machine. String constants (not an enum) so new
/// states can be added without breaking older fleet clients.
/// </summary>
public static class SessionStates
{
    /// <summary>Attract loop is running; no customer engaged.</summary>
    public const string Attract = "attract";

    /// <summary>Customer engaged; service selection (sell/pawn) in progress.</summary>
    public const string Welcome = "welcome";

    /// <summary>Tray is open or moving; customer is placing the item.</summary>
    public const string PlacingItem = "placing_item";

    /// <summary>Item analysis (detection, authenticity, pricing) in progress.</summary>
    public const string Analyzing = "analyzing";

    /// <summary>An offer is on screen with its lock countdown.</summary>
    public const string Offer = "offer";

    /// <summary>Hardware-driven identity sequence (ID scan, face match, fingerprint, signature).</summary>
    public const string Identity = "identity";

    /// <summary>Contact and receipt-channel capture.</summary>
    public const string Contact = "contact";

    /// <summary>Payout method selection.</summary>
    public const string Payout = "payout";

    /// <summary>Payout method confirmed; awaiting settle command.</summary>
    public const string PayoutConfirmed = "payout_confirmed";

    /// <summary>Bagging and dispensing/transferring in progress.</summary>
    public const string Settling = "settling";

    /// <summary>The item is being returned to the customer.</summary>
    public const string ReturningItem = "returning_item";

    /// <summary>Terminal state; the session is complete.</summary>
    public const string Done = "done";
}
