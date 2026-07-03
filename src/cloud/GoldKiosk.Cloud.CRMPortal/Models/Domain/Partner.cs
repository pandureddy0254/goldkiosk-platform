using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>crm.partners</c> — a won lead converted into a commercial partner.</summary>
public class Partner
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>Registered legal entity name.</summary>
    public string LegalName { get; set; } = string.Empty;

    /// <summary>Trading/display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// jsonb in Postgres — <see cref="JObject"/> so the deserializer doesn't choke trying to fit
    /// a JSON object into a string.
    /// </summary>
    public JObject? BillingAddress { get; set; }

    /// <summary>Operating region, when known.</summary>
    public string? Region { get; set; }

    /// <summary>Primary admin contact name.</summary>
    public string PrimaryAdminName { get; set; } = string.Empty;

    /// <summary>Primary admin contact email (citext column).</summary>
    public string PrimaryAdminEmail { get; set; } = string.Empty;

    /// <summary>Primary admin's role title, when known.</summary>
    public string? PrimaryAdminRoleTitle { get; set; }

    /// <summary>Number of kiosks in the initial order.</summary>
    public int InitialKioskCount { get; set; }

    /// <summary>Snapshot MRR amount at conversion time (live MRR comes from subscriptions).</summary>
    public decimal MrrAmount { get; set; }

    /// <summary>ISO currency code for <see cref="MrrAmount"/>.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Tenant lifecycle status — one of <see cref="TenantStatus"/>.</summary>
    public string TenantStatus { get; set; } = "provisioning";

    /// <summary>When the partner's tenant finished provisioning.</summary>
    public DateTime? ProvisionedAt { get; set; }

    /// <summary>When the partner churned, if ever.</summary>
    public DateTime? ChurnedAt { get; set; }

    /// <summary>The originating lead, when converted from one.</summary>
    public Guid? LeadId { get; set; }

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>One-line display of the billing address. Returns empty string if unset.</summary>
    [NotMapped]
    public string BillingAddressLine =>
        BillingAddress is null
            ? string.Empty
            : string.Join(" · ", new[]
              {
                  BillingAddress["line1"]?.ToString(),
                  BillingAddress["city"]?.ToString(),
                  BillingAddress["country"]?.ToString()
              }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
