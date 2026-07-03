namespace GoldKiosk.Cloud.CRMPortal.Models;

/// <summary>View model for the shared error page.</summary>
public class ErrorViewModel
{
    /// <summary>The request/trace identifier shown to help support correlate logs.</summary>
    public string? RequestId { get; set; }

    /// <summary>True when a <see cref="RequestId"/> is available to display.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
