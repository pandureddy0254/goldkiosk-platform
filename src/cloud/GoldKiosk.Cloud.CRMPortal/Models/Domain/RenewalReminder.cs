namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>billing.renewal_reminders</c> — a scheduled renewal notification.</summary>
public class RenewalReminder
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The subscription the reminder is about.</summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>Reminder kind relative to renewal (e.g. <c>T-30</c>, <c>T-7</c>).</summary>
    public string Kind { get; set; } = "T-30";

    /// <summary>The date the reminder is scheduled to send (DATE column).</summary>
    public DateTime ScheduledFor { get; set; }

    /// <summary>Recipient email address (citext column).</summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>Delivery state: queued | sent | failed.</summary>
    public string DeliveryStatus { get; set; } = "queued";

    /// <summary>Email provider's message id after send.</summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>When the reminder was actually sent.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Failure detail when delivery failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Row creation timestamp (DB default; never written by the app).</summary>
    public DateTime CreatedAt { get; set; }
}
