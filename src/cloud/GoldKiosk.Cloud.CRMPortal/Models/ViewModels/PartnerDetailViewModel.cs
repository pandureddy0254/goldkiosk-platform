using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>View model for the partner detail page.</summary>
public class PartnerDetailViewModel
{
    /// <summary>The partner being displayed.</summary>
    public Partner Partner { get; set; } = default!;

    /// <summary>The partner's tenant row, when provisioned.</summary>
    public Tenant? Tenant { get; set; }

    /// <summary>The partner's most recent subscription, when any.</summary>
    public Subscription? Subscription { get; set; }

    /// <summary>Every activation key issued for the tenant, newest first.</summary>
    public List<ActivationKey> KeyHistory { get; set; } = [];
}
