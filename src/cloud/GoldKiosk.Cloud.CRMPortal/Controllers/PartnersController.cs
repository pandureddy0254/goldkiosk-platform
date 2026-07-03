using System.Globalization;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Models.ViewModels;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Partner directory, partner detail, and license key reissue/revoke actions.</summary>
public class PartnersController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILicenseSigner _signer;
    private readonly IEmailService _email;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    /// <param name="signer">License token signer.</param>
    /// <param name="email">License delivery email service.</param>
    public PartnersController(
        CrmDbContext db,
        ICurrentUserService currentUser,
        ILicenseSigner signer,
        IEmailService email)
    {
        _db = db;
        _currentUser = currentUser;
        _signer = signer;
        _email = email;
    }

    // Row shape returned by crm.record_issued_license.
    private sealed record IssuedRpcRow(Guid OutTenantId, Guid OutActivationKeyId);

    private IQueryable<Partner> ScopedPartners()
    {
        var q = _db.Partners.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(p => _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid));
        }

        return q;
    }

    /// <summary>Directory list + KPI strip + filter bar.</summary>
    /// <param name="search">Legal-name search.</param>
    /// <param name="status">Tenant status filter or "all".</param>
    /// <param name="region">Region substring filter or "all".</param>
    /// <param name="keyState">Latest-key state filter or "all".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? region,
        string? keyState,
        CancellationToken ct)
    {
        ViewData["Title"] = "Partners";
        ViewData["ActiveSection"] = "partners";

        var partnersQuery = ScopedPartners();

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            // Hoisted out of the expression tree: satisfies CA1862 and keeps the
            // predicate translatable by EF (string.Equals+StringComparison is not).
            var statusLower = status.ToLowerInvariant();
            partnersQuery = partnersQuery.Where(p => p.TenantStatus == statusLower);
        }

        if (!string.IsNullOrWhiteSpace(region) && !string.Equals(region, "all", StringComparison.OrdinalIgnoreCase))
        {
            partnersQuery = partnersQuery.Where(p => p.Region != null && EF.Functions.ILike(p.Region, $"%{region}%"));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            partnersQuery = partnersQuery.Where(p => EF.Functions.ILike(p.LegalName, $"%{search}%"));
        }

        var partners = await partnersQuery.OrderByDescending(p => p.MrrAmount).ToListAsync(ct);

        // Activation keys (managers only see these via RLS); group by partner.
        var keys = await _db.ActivationKeys.AsNoTracking().ToListAsync(ct);

        // Map tenant_id -> partner_id
        var tenantToPartner = await _db.Tenants.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.PartnerId, ct);

        // Prefer the latest live (not revoked, not consumed) key per partner.
        var keysByPartner = keys
            .Where(k => tenantToPartner.ContainsKey(k.TenantId))
            .GroupBy(k => tenantToPartner[k.TenantId])
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(k => k.RevokedAt is null && k.ConsumedAt is null)
                .ThenByDescending(k => k.IssuedAt)
                .First());

        // --- Live MRR from subscriptions ---
        var liveSubs = await _db.Subscriptions.AsNoTracking()
            .Where(s => s.Status == "trialing" || s.Status == "active" || s.Status == "past_due")
            .Take(2000)
            .ToListAsync(ct);
        var subsByPartner = liveSubs
            .GroupBy(s => s.PartnerId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Mrr = g.Sum(s => s.MrrAmount),
                    Currency = g.GroupBy(s => s.CurrencyCode)
                                .OrderByDescending(grp => grp.Sum(x => x.MrrAmount))
                                .First().Key,
                });

        var rows = partners.Select(p =>
        {
            subsByPartner.TryGetValue(p.Id, out var liveSub);
            return new PartnerRow
            {
                Partner = p,
                LatestKey = keysByPartner.TryGetValue(p.Id, out var k) ? k : null,
                LiveMrr = liveSub?.Mrr ?? 0m,
                LiveCurrency = liveSub?.Currency ?? p.CurrencyCode ?? "USD",
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyState) && !string.Equals(keyState, "all", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(r => string.Equals(r.LatestKey?.State, keyState, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var liveMrrByCurrency = rows
            .Where(r => r.LiveMrr > 0)
            .GroupBy(r => r.LiveCurrency)
            .OrderByDescending(g => g.Sum(r => r.LiveMrr))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.LiveMrr));

        var vm = new PartnerDirectoryViewModel
        {
            Rows = rows,
            ActiveCount = partners.Count(p => p.TenantStatus == TenantStatus.Active),
            TotalCount = partners.Count,
            KiosksDeployed = partners.Sum(p => p.InitialKioskCount),
            LiveMrrByCurrency = liveMrrByCurrency,
            KeysOutstanding = rows.Count(r => r.LatestKey is { ConsumedAt: null, RevokedAt: null } &&
                                              r.LatestKey.ExpiresAt > DateTime.UtcNow),
            Search = search,
            Status = status,
            Region = region,
            KeyState = keyState,
        };
        return View(vm);
    }

    /// <summary>Partner detail — KPI strip + two-column rail.</summary>
    /// <param name="id">Partner id.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        ViewData["Title"] = "Partner detail";
        ViewData["ActiveSection"] = "partners";

        ViewBag.IsManager = _currentUser.IsManager;

        var partner = await ScopedPartners().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (partner is null)
        {
            return NotFound();
        }

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.PartnerId == id, ct);

        var sub = await _db.Subscriptions.AsNoTracking()
            .Where(s => s.PartnerId == id)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var keys = new List<ActivationKey>();
        if (tenant is not null)
        {
            keys = await _db.ActivationKeys.AsNoTracking()
                .Where(k => k.TenantId == tenant.Id)
                .OrderByDescending(k => k.IssuedAt)
                .ToListAsync(ct);
        }

        return View(new PartnerDetailViewModel
        {
            Partner = partner,
            Tenant = tenant,
            Subscription = sub,
            KeyHistory = keys,
        });
    }

    /// <summary>
    /// Manager-only. Generates a new Ed25519-signed license token, records the hash
    /// for audit, and supersedes the previous live key.
    /// </summary>
    /// <param name="tenantId">Tenant the key is issued for.</param>
    /// <param name="partnerId">Partner the key belongs to.</param>
    /// <param name="reason">Mandatory reason recorded with the reissue.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken, RequireManager]
    public async Task<IActionResult> ReissueKey(Guid tenantId, Guid partnerId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ReissueError"] = "A reason is required.";
            return RedirectToAction(nameof(Details), new { id = partnerId });
        }

        var partner = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partnerId, ct);
        if (partner is null)
        {
            TempData["ReissueError"] = "Partner not found.";
            return RedirectToAction(nameof(Details), new { id = partnerId });
        }

        var issued = _signer.Issue(new LicensePayload
        {
            TenantId = tenantId,
            PartnerId = partnerId,
            LegalName = partner.LegalName,
            DisplayName = string.IsNullOrWhiteSpace(partner.DisplayName) ? partner.LegalName : partner.DisplayName,
            PlanCode = "enterprise",
            Features = ["kiosk_ops", "billing", "audit_log"],
            KioskCap = partner.InitialKioskCount,
            RegionHint = partner.Region ?? "",
            AdminEmail = partner.PrimaryAdminEmail,
        });

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(issued.Payload.ExpiresAt).UtcDateTime;
        await _db.Database
            .SqlQueryRaw<IssuedRpcRow>(
                "SELECT out_tenant_id, out_activation_key_id " +
                "FROM crm.record_issued_license({0}, {1}, {2}, {3}, {4}, {5})",
                partnerId, issued.KeyPrefix, issued.Sha256Hex, partner.PrimaryAdminEmail, expiresAt, "ae-1")
            .ToListAsync(ct);

        TempData["RevealKey"] = issued.Token;
        TempData["RevealPrefix"] = issued.KeyPrefix;
        TempData["RevealExpires"] = expiresAt.ToString("O", CultureInfo.InvariantCulture);
        TempData["RevealPartner"] = partnerId.ToString();
        TempData["ReissueReason"] = reason;

        var emailResult = await _email.SendLicenseEmailAsync(
            partner.PrimaryAdminEmail,
            string.IsNullOrWhiteSpace(partner.PrimaryAdminName) ? "there" : partner.PrimaryAdminName,
            issued.Payload,
            issued.Token,
            ct);
        if (emailResult.Success)
        {
            TempData["EmailFlash"] = $"Re-issued license emailed to {partner.PrimaryAdminEmail}.";
        }
        else
        {
            TempData["EmailError"] = $"License re-issued but email to {partner.PrimaryAdminEmail} failed: {emailResult.Error}";
        }

        return RedirectToAction(nameof(Details), new { id = partnerId });
    }

    /// <summary>Manager-only. Adds the key prefix to the revocation list.</summary>
    /// <param name="keyPrefix">The <c>AIKI-XXXXXXXX</c> prefix to revoke.</param>
    /// <param name="partnerId">Partner the key belongs to (for the redirect).</param>
    /// <param name="reason">Mandatory reason recorded in the audit log.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken, RequireManager]
    public async Task<IActionResult> RevokeKey(string keyPrefix, Guid partnerId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix) || string.IsNullOrWhiteSpace(reason))
        {
            TempData["RevokeError"] = "Key prefix and reason are required.";
            return RedirectToAction(nameof(Details), new { id = partnerId });
        }

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT crm.revoke_license_key({keyPrefix}, {reason})", ct);

        TempData["RevokeFlash"] = $"Revoked {keyPrefix} · reason recorded in audit_log.";
        return RedirectToAction(nameof(Details), new { id = partnerId });
    }
}
