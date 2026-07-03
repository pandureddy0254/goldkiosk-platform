using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Merchant management controller.</summary>
[Authorize]
public class MerchantManagementController(
    IMerchantService merchantService,
    IFranchiseWalletService walletService,
    ILogger<MerchantManagementController> logger) : Controller
{
    // ───────────────────────────── Merchants ──────────────────────────────

    /// <summary>Manage merchants.</summary>
    [HttpGet]
    [Permission("merchants:read")]
    public async Task<IActionResult> ManageMerchants(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await merchantService.ListAsync(search, stat, pageSize, pageNo, ct);

        ViewBag.KycStatusDropdown = PortalHelpers.GetKycStatusDropdown();
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add merchant.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("merchants:write")]
    public async Task<IActionResult> AddMerchant(MerchantViewModel vm, CancellationToken ct = default)
    {
        var result = await merchantService.AddAsync(vm, ct);
        if (!result.Success)
        {
            logger.AddMerchantFailed(result.ErrorSummary);
        }

        TempData[result.Success ? "MerchantInfo" : "MerchantError"]
            = result.Success ? $"Merchant '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(ManageMerchants));
    }

    /// <summary>Edit merchant.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("merchants:write")]
    public async Task<IActionResult> EditMerchant(MerchantViewModel vm, CancellationToken ct = default)
    {
        var result = await merchantService.EditAsync(vm, ct);
        TempData[result.Success ? "MerchantInfo" : "MerchantError"]
            = result.Success ? $"Merchant '{vm.Code}' updated." : result.ErrorSummary;
        return RedirectToAction(nameof(ManageMerchants));
    }

    /// <summary>Delete merchant.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("merchants:write")]
    public async Task<IActionResult> DeleteMerchant(Guid id, CancellationToken ct = default)
    {
        var result = await merchantService.DeleteAsync(id, ct);
        TempData[result.Success ? "MerchantInfo" : "MerchantError"]
            = result.Success ? "Merchant deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(ManageMerchants));
    }

    // ────────────────────────── Franchise wallets ─────────────────────────

    /// <summary>Franchises wallets.</summary>
    [HttpGet]
    [Permission("merchants:read")]
    public async Task<IActionResult> FranchisesWallets(string? search, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await walletService.ListAsync(search, pageSize, pageNo, ct);

        ViewBag.TopupStatusDropdown = PortalHelpers.GetWalletTopupStatusDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        return View(models);
    }

    /// <summary>Approve topup.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("merchants:write")]
    public async Task<IActionResult> ApproveTopup(Guid topupId, CancellationToken ct = default)
    {
        var result = await walletService.ApproveTopupAsync(topupId, ct);
        TempData[result.Success ? "WalletInfo" : "WalletError"]
            = result.Success ? "Topup approved." : result.ErrorSummary;
        return RedirectToAction(nameof(FranchisesWallets));
    }

    /// <summary>Reject topup.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("merchants:write")]
    public async Task<IActionResult> RejectTopup(Guid topupId, string? reason, CancellationToken ct = default)
    {
        var result = await walletService.RejectTopupAsync(topupId, reason, ct);
        TempData[result.Success ? "WalletInfo" : "WalletError"]
            = result.Success ? "Topup rejected." : result.ErrorSummary;
        return RedirectToAction(nameof(FranchisesWallets));
    }
}
