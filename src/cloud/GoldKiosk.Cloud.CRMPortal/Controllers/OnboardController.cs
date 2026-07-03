using System.Text.Json;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Logging;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Models.ViewModels;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>
/// Four-step partner onboarding wizard ending in an offline Ed25519 license issuance.
/// Wizard state is carried in TempData between steps.
/// </summary>
public class OnboardController : Controller
{
    private const string TempKey = "OnboardWizard";
    private readonly CrmDbContext _db;
    private readonly ILicenseSigner _signer;
    private readonly IEmailService _email;
    private readonly ILogger<OnboardController> _logger;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="signer">License token signer.</param>
    /// <param name="email">License delivery email service.</param>
    /// <param name="logger">Logger (ids only — never emails).</param>
    public OnboardController(CrmDbContext db, ILicenseSigner signer, IEmailService email, ILogger<OnboardController> logger)
    {
        _db = db;
        _signer = signer;
        _email = email;
        _logger = logger;
    }

    // Row shape returned by crm.mark_lead_won_and_create_partner (scalar uuid).
    private sealed record PartnerIdRow(Guid Value);

    // Row shape returned by crm.ensure_tenant_for_partner (scalar uuid).
    private sealed record TenantIdRow(Guid Value);

    // Row shape returned by crm.record_issued_license.
    private sealed record IssuedRpcRow(Guid OutTenantId, Guid OutActivationKeyId);

    /// <summary>Renders the wizard at the current (or requested) step.</summary>
    /// <param name="leadId">Optional originating lead.</param>
    /// <param name="step">Optional explicit step (clamped 1–4).</param>
    public IActionResult Index(Guid? leadId, int? step)
    {
        ViewData["Title"] = "Onboard partner";
        ViewData["ActiveSection"] = "onboard";

        var vm = ReadFromTemp() ?? new OnboardWizardViewModel();
        if (leadId.HasValue)
        {
            vm.LeadId = leadId;
        }

        if (step.HasValue)
        {
            vm.CurrentStep = Math.Clamp(step.Value, 1, 4);
        }

        SaveToTemp(vm);
        return View(vm);
    }

    /// <summary>Saves step 1 (legal entity &amp; billing) and advances to step 2.</summary>
    /// <param name="input">Posted wizard fields.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Step1(OnboardWizardViewModel input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var vm = ReadFromTemp() ?? new OnboardWizardViewModel();
        vm.LegalName = input.LegalName;
        vm.DisplayName = input.DisplayName;
        vm.VatTrn = input.VatTrn;
        vm.BillingAddress = input.BillingAddress;
        vm.Region = input.Region;
        vm.CurrencyCode = string.IsNullOrWhiteSpace(input.CurrencyCode) ? "USD" : input.CurrencyCode;
        vm.LeadId = input.LeadId ?? vm.LeadId;
        vm.CurrentStep = 2;
        SaveToTemp(vm);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Saves step 2 (primary admin) and advances to step 3.</summary>
    /// <param name="input">Posted wizard fields.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Step2(OnboardWizardViewModel input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var vm = ReadFromTemp() ?? new OnboardWizardViewModel();
        vm.AdminFullName = input.AdminFullName;
        vm.AdminEmail = input.AdminEmail;
        vm.AdminRoleTitle = input.AdminRoleTitle;
        vm.AdminMobile = input.AdminMobile;
        vm.CurrentStep = 3;
        SaveToTemp(vm);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Saves step 3 (initial kiosk order); stays on step 3 so Provision is visible.</summary>
    /// <param name="input">Posted wizard fields.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Step3(OnboardWizardViewModel input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var vm = ReadFromTemp() ?? new OnboardWizardViewModel();
        vm.InitialKioskCount = input.InitialKioskCount;
        vm.Sites = input.Sites;
        vm.Phasing = input.Phasing;
        vm.Term = input.Term;
        vm.CurrentStep = 3;   // stay on step 3 so the Provision button is visible
        SaveToTemp(vm);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Final POST — issues an offline-verifiable Ed25519 license token and records
    /// the audit row. Manager-only: issuing a key creates a real credential.
    /// <para>
    /// The whole flow (create partner → resolve tenant → record license) runs in ONE
    /// database transaction, and the authoritative tenant id is obtained via
    /// <c>crm.ensure_tenant_for_partner</c> BEFORE signing, so the token is signed
    /// exactly once with the real tenant id (the legacy double-sign is gone).
    /// </para>
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken, RequireManager]
    public async Task<IActionResult> Provision(CancellationToken ct)
    {
        var vm = ReadFromTemp();
        if (vm is null || string.IsNullOrWhiteSpace(vm.LegalName) || string.IsNullOrWhiteSpace(vm.AdminEmail))
        {
            TempData["OnboardError"] = "Legal entity and admin email are required to issue a license.";
            return RedirectToAction(nameof(Index));
        }

        IssuedLicense issued;
        Guid partnerId;
        Guid tenantId;
        DateTime expiresAt;

        // One transaction around partner creation + tenant resolution + license
        // recording: a failure anywhere rolls the whole provision back, so no
        // half-provisioned partner or orphaned key row can survive.
        var tx = await _db.Database.BeginTransactionAsync(ct);
        await using (tx)
        {
            // ── 1. Resolve or create the partner row ───────────────────────────
            if (vm.LeadId.HasValue)
            {
                // Build the billing payload as a Newtonsoft JObject and pass it as
                // jsonb text to the RPC.
                var billing = new JObject
                {
                    ["line1"] = vm.BillingAddress ?? "",
                    ["vat"] = vm.VatTrn ?? "",
                    ["currency"] = vm.CurrencyCode,
                };

                var partnerRows = await _db.Database
                    .SqlQueryRaw<PartnerIdRow>(
                        "SELECT crm.mark_lead_won_and_create_partner({0}, {1}, {2}::jsonb, {3}, {4}, {5}, {6}) AS value",
                        vm.LeadId.Value,
                        vm.LegalName,
                        billing.ToString(Newtonsoft.Json.Formatting.None),
                        vm.AdminFullName ?? "",
                        vm.AdminEmail,
                        (object?)vm.AdminRoleTitle ?? DBNull.Value,
                        Math.Max(0, vm.InitialKioskCount))
                    .ToListAsync(ct);

                var created = partnerRows.FirstOrDefault();
                if (created is null || created.Value == Guid.Empty)
                {
                    await tx.RollbackAsync(ct);
                    TempData["OnboardError"] = "Partner creation RPC returned an unexpected result.";
                    return RedirectToAction(nameof(Index));
                }

                partnerId = created.Value;
            }
            else
            {
                var partner = new Partner
                {
                    LegalName = vm.LegalName,
                    DisplayName = string.IsNullOrWhiteSpace(vm.DisplayName) ? vm.LegalName : vm.DisplayName,
                    BillingAddress = JObject.FromObject(new { line1 = vm.BillingAddress ?? "", vat = vm.VatTrn ?? "" }),
                    Region = vm.Region,
                    PrimaryAdminName = vm.AdminFullName ?? "",
                    PrimaryAdminEmail = vm.AdminEmail,
                    PrimaryAdminRoleTitle = vm.AdminRoleTitle,
                    InitialKioskCount = Math.Max(0, vm.InitialKioskCount),
                    CurrencyCode = vm.CurrencyCode,
                };
                _db.Partners.Add(partner);
                await _db.SaveChangesAsync(ct);
                partnerId = partner.Id;
            }

            // ── 2. Get the authoritative tenant id BEFORE signing ──────────────
            var tenantRows = await _db.Database
                .SqlQueryRaw<TenantIdRow>(
                    "SELECT crm.ensure_tenant_for_partner({0}, {1}) AS value",
                    partnerId, "ae-1")
                .ToListAsync(ct);

            tenantId = tenantRows.FirstOrDefault()?.Value ?? Guid.Empty;
            if (tenantId == Guid.Empty)
            {
                await tx.RollbackAsync(ct);
                TempData["OnboardError"] = "Tenant provisioning RPC returned an unexpected result.";
                return RedirectToAction(nameof(Index));
            }

            // ── 3. Sign the license payload exactly once, with the real ids ────
            var planCode = string.IsNullOrWhiteSpace(vm.Term) ? "starter" : "enterprise";
            var payload = new LicensePayload
            {
                TenantId = tenantId,
                PartnerId = partnerId,
                LegalName = vm.LegalName,
                DisplayName = string.IsNullOrWhiteSpace(vm.DisplayName) ? vm.LegalName : vm.DisplayName,
                PlanCode = planCode,
                Features = ["kiosk_ops", "billing", "audit_log"],
                KioskCap = Math.Max(0, vm.InitialKioskCount),
                RegionHint = vm.Region ?? "",
                AdminEmail = vm.AdminEmail,
            };
            issued = _signer.Issue(payload);

            // ── 4. Record the issuance (writes activation_keys + audit) ────────
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(issued.Payload.ExpiresAt).UtcDateTime;
            await _db.Database
                .SqlQueryRaw<IssuedRpcRow>(
                    "SELECT out_tenant_id, out_activation_key_id " +
                    "FROM crm.record_issued_license({0}, {1}, {2}, {3}, {4}, {5})",
                    partnerId, issued.KeyPrefix, issued.Sha256Hex, vm.AdminEmail, expiresAt, "ae-1")
                .ToListAsync(ct);

            await tx.CommitAsync(ct);
        }

        _logger.LicenseIssued(partnerId, tenantId, issued.KeyPrefix);

        vm.Reveal = new ActivationKeyRevealViewModel
        {
            ActivationKey = issued.Token,
            KeyPrefix = issued.KeyPrefix,
            ExpiresAt = expiresAt,
            TenantId = tenantId,
            PartnerId = partnerId,
            PartnerName = vm.LegalName,
            AdminEmail = vm.AdminEmail,
            RegionCode = "ae-1",
            Sha256Prefix = issued.Sha256Hex[..4] + "…" + issued.Sha256Hex[^4..],
        };
        vm.CurrentStep = 4;
        SaveToTemp(vm);

        // ── 5. Email the license to the primary admin via SES (outside the tx —
        // the issuance is already durable; email failure only shows a flash).
        var emailResult = await _email.SendLicenseEmailAsync(
            vm.AdminEmail,
            string.IsNullOrWhiteSpace(vm.AdminFullName) ? "there" : vm.AdminFullName!,
            issued.Payload,
            issued.Token,
            ct);
        if (emailResult.Success)
        {
            TempData["EmailFlash"] = $"License emailed to {vm.AdminEmail} · message-id {emailResult.MessageId}.";
        }
        else
        {
            TempData["EmailError"] = $"License generated but email to {vm.AdminEmail} failed: {emailResult.Error}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Clears the wizard state.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        TempData.Remove(TempKey);
        return RedirectToAction(nameof(Index));
    }

    private OnboardWizardViewModel? ReadFromTemp()
    {
        if (!TempData.TryGetValue(TempKey, out var raw) || raw is not string json)
        {
            return null;
        }

        TempData.Keep(TempKey);
        try
        {
            return JsonSerializer.Deserialize<OnboardWizardViewModel>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void SaveToTemp(OnboardWizardViewModel vm)
        => TempData[TempKey] = JsonSerializer.Serialize(vm);
}
