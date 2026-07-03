using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>State carried through the four-step partner onboarding wizard (persisted in TempData).</summary>
public class OnboardWizardViewModel
{
    /// <summary>The wizard step to render (1–4).</summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>The originating lead, when the wizard was launched from one.</summary>
    public Guid? LeadId { get; set; }

    /// <summary>Step 1 — registered legal entity name.</summary>
    [Display(Name = "Legal name")]
    public string? LegalName { get; set; }

    /// <summary>Step 1 — trading/display name.</summary>
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    /// <summary>Step 1 — VAT / TRN registration number.</summary>
    public string? VatTrn { get; set; }

    /// <summary>Step 1 — billing address line.</summary>
    public string? BillingAddress { get; set; }

    /// <summary>Step 1 — operating region.</summary>
    public string? Region { get; set; }

    /// <summary>Step 1 — billing currency code.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Step 2 — primary admin full name.</summary>
    [Display(Name = "Full name")]
    public string? AdminFullName { get; set; }

    /// <summary>Step 2 — primary admin email (license delivery address).</summary>
    [EmailAddress]
    public string? AdminEmail { get; set; }

    /// <summary>Step 2 — primary admin role title.</summary>
    public string? AdminRoleTitle { get; set; }

    /// <summary>Step 2 — primary admin mobile number.</summary>
    public string? AdminMobile { get; set; }

    /// <summary>Step 3 — number of kiosks in the initial order.</summary>
    public int InitialKioskCount { get; set; }

    /// <summary>Step 3 — deployment sites description.</summary>
    public string? Sites { get; set; }

    /// <summary>Step 3 — rollout phasing description.</summary>
    public string? Phasing { get; set; }

    /// <summary>Step 3 — commercial term description.</summary>
    public string? Term { get; set; }

    /// <summary>Step 4 — the one-time reveal (populated after the Provision RPC).</summary>
    public ActivationKeyRevealViewModel? Reveal { get; set; }
}
