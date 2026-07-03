using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Voucher management controller.</summary>
[Authorize]
public class VoucherManagementController(
    IVoucherService voucherService,
    ILogger<VoucherManagementController> logger) : Controller
{
    // ──────────────────────────── Vouchers ─────────────────────────────────

    /// <summary>Voucher configuration.</summary>
    [HttpGet]
    [Permission("vouchers:read")]
    public async Task<IActionResult> VoucherConfiguration(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await voucherService.ListVouchersAsync(search, stat, pageSize, pageNo, ct);

        ViewBag.DiscountTypeDropdown = PortalHelpers.GetVoucherDiscountTypeDropdown();
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add voucher.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> AddVoucher(VoucherViewModel vm, CancellationToken ct = default)
    {
        var result = await voucherService.AddVoucherAsync(vm, ct);
        if (!result.Success)
        {
            logger.AddVoucherFailed(result.ErrorSummary);
        }

        TempData[result.Success ? "VoucherInfo" : "VoucherError"]
            = result.Success ? $"Voucher '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(VoucherConfiguration));
    }

    /// <summary>Edit voucher.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> EditVoucher(VoucherViewModel vm, CancellationToken ct = default)
    {
        var result = await voucherService.EditVoucherAsync(vm, ct);
        TempData[result.Success ? "VoucherInfo" : "VoucherError"]
            = result.Success ? $"Voucher '{vm.Code}' updated." : result.ErrorSummary;
        return RedirectToAction(nameof(VoucherConfiguration));
    }

    /// <summary>Delete voucher.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> DeleteVoucher(Guid id, CancellationToken ct = default)
    {
        var result = await voucherService.DeleteVoucherAsync(id, ct);
        TempData[result.Success ? "VoucherInfo" : "VoucherError"]
            = result.Success ? "Voucher deactivated." : result.ErrorSummary;
        return RedirectToAction(nameof(VoucherConfiguration));
    }

    // ──────────────────────── Redemption policies ──────────────────────────

    /// <summary>Redemption policies.</summary>
    [HttpGet]
    [Permission("vouchers:read")]
    public async Task<IActionResult> RedemptionPolicies(string? search, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await voucherService.ListPoliciesAsync(search, pageSize, pageNo, ct);

        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        return View(models);
    }

    /// <summary>Add redemption policy.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> AddRedemptionPolicy(RedemptionPolicyViewModel vm, CancellationToken ct = default)
    {
        var result = await voucherService.AddPolicyAsync(vm, ct);
        TempData[result.Success ? "PolicyInfo" : "PolicyError"]
            = result.Success ? "Redemption policy created." : result.ErrorSummary;
        return RedirectToAction(nameof(RedemptionPolicies));
    }

    /// <summary>Edit redemption policy.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> EditRedemptionPolicy(RedemptionPolicyViewModel vm, CancellationToken ct = default)
    {
        var result = await voucherService.EditPolicyAsync(vm, ct);
        TempData[result.Success ? "PolicyInfo" : "PolicyError"]
            = result.Success ? "Redemption policy updated." : result.ErrorSummary;
        return RedirectToAction(nameof(RedemptionPolicies));
    }

    /// <summary>Delete redemption policy.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("vouchers:write")]
    public async Task<IActionResult> DeleteRedemptionPolicy(Guid id, CancellationToken ct = default)
    {
        var result = await voucherService.DeletePolicyAsync(id, ct);
        TempData[result.Success ? "PolicyInfo" : "PolicyError"]
            = result.Success ? "Redemption policy deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(RedemptionPolicies));
    }

    // ──────────────────────── Voucher redemptions (read-only) ──────────────

    /// <summary>Voucher transaction.</summary>
    [HttpGet]
    [Permission("vouchers:read")]
    public async Task<IActionResult> VoucherTransaction(string? search, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await voucherService.ListRedemptionsAsync(search, pageSize, pageNo, ct);

        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        return View(models);
    }
}
