using System.ComponentModel.DataAnnotations.Schema;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>billing.subscriptions</c> — a partner's recurring licence subscription.</summary>
public class Subscription
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The subscribed partner.</summary>
    public Guid PartnerId { get; set; }

    /// <summary>Commercial plan code (e.g. basic, enterprise).</summary>
    public string PlanCode { get; set; } = "basic";

    /// <summary>Contracted annual amount in <see cref="CurrencyCode"/>.</summary>
    public decimal AnnualAmount { get; set; }

    /// <summary>ISO currency code for the monetary columns.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Billing cadence — one of <see cref="BillingCycle"/>.</summary>
    public string BillingCycle { get; set; } = "annual";

    /// <summary>Lifecycle status — one of <see cref="SubscriptionStatus"/>.</summary>
    public string Status { get; set; } = "trialing";

    /// <summary>Whether the subscription renews automatically.</summary>
    public bool AutoRenew { get; set; }

    /// <summary>How the partner pays (e.g. bank_transfer, card).</summary>
    public string PaymentMethod { get; set; } = "bank_transfer";

    /// <summary>When the subscription started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Start of the current billing period.</summary>
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>End of the current billing period (renewal date).</summary>
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>When the trial ends, when trialing.</summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>When cancellation was requested, if ever.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Reason recorded at cancellation.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>When the cancellation takes effect (null = end of current period).</summary>
    public DateTime? CancellationEffectiveAt { get; set; }

    /// <summary>When the last renewal was applied.</summary>
    public DateTime? LastRenewalAt { get; set; }

    /// <summary>External billing provider's customer id, when linked.</summary>
    public string? ExternalCustomerId { get; set; }

    /// <summary>External billing provider's subscription id, when linked.</summary>
    public string? ExternalSubscriptionId { get; set; }

    /// <summary>Free-form notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Monthly recurring revenue derived from <see cref="AnnualAmount"/>.</summary>
    [NotMapped]
    public decimal MrrAmount => Math.Round(AnnualAmount / 12m, 2);

    /// <summary>Whole days until <see cref="CurrentPeriodEnd"/> (negative when past due).</summary>
    [NotMapped]
    public int DaysUntilRenewal => (int)Math.Ceiling((CurrentPeriodEnd - DateTime.UtcNow).TotalDays);
}
