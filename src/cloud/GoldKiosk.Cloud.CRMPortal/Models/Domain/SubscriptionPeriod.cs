namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>billing.subscription_periods</c> — one invoiced billing period.</summary>
public class SubscriptionPeriod
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The owning subscription.</summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>Period start (inclusive).</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Period end (exclusive).</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>Invoiced amount in <see cref="CurrencyCode"/>.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO currency code for <see cref="Amount"/>.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Invoice status: pending | invoiced | paid | refunded.</summary>
    public string Status { get; set; } = "pending";

    /// <summary>External billing provider's invoice id, when linked.</summary>
    public string? ExternalInvoiceId { get; set; }

    /// <summary>External billing provider's payment id, when linked.</summary>
    public string? ExternalPaymentId { get; set; }

    /// <summary>When the invoice was raised.</summary>
    public DateTime? InvoicedAt { get; set; }

    /// <summary>When the invoice was paid.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>When the payment was refunded, if ever.</summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>Row creation timestamp (DB default; never written by the app).</summary>
    public DateTime CreatedAt { get; set; }
}
