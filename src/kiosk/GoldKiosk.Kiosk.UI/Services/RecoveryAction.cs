namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// What a recovery button on the error overlay does. Navigation always follows the
/// server's session state — recovery actions only issue commands or dismiss.
/// </summary>
public enum RecoveryAction
{
    /// <summary>Dismiss the overlay; the flow follows the session state.</summary>
    Dismiss,

    /// <summary>Abort with <c>user_cancel</c> and return the held item.</summary>
    ReturnItem,

    /// <summary>Abort the session with <c>user_cancel</c>.</summary>
    EndSession,

    /// <summary>Reset the local session and go back to the attract loop.</summary>
    BackToStart,
}
