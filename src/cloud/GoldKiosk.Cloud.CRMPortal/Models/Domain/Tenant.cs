namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>crm.tenants</c> — the provisioned Admin Dashboard tenant for a partner.</summary>
public class Tenant
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The partner this tenant belongs to (unique per partner).</summary>
    public Guid PartnerId { get; set; }

    /// <summary>Correlated tenant id in the Admin Dashboard, when linked.</summary>
    public Guid? AdminTenantId { get; set; }

    /// <summary>Deployment region code (e.g. <c>ae-1</c>).</summary>
    public string RegionCode { get; set; } = "ae-1";

    /// <summary>Provisioning state: pending | succeeded | failed.</summary>
    public string ProvisioningStatus { get; set; } = "pending";

    /// <summary>Failure detail when <see cref="ProvisioningStatus"/> is <c>failed</c>.</summary>
    public string? ProvisioningError { get; set; }

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }
}
