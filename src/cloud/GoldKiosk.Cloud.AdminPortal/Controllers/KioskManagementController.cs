using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Kiosk management controller.</summary>
[Authorize]
public class KioskManagementController(
    IKioskService kioskService,
    IScreenSaverService screenSaverService,
    ILogger<KioskManagementController> logger) : Controller
{
    // ───────────────────────────── Kiosks ─────────────────────────────────────

    /// <summary>Kiosk configuration.</summary>
    [HttpGet]
    [Permission("kiosks:read")]
    public async Task<IActionResult> KioskConfiguration(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await kioskService.ListAsync(search, stat, pageSize, pageNo, ct);

        ViewBag.KioskStatusDropdown = PortalHelpers.GetKioskStatusDropdown();
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.MaintenanceDropdown = PortalHelpers.GetMaintenanceDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add kiosk.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:write")]
    public async Task<IActionResult> AddKiosk(KioskMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await kioskService.AddAsync(vm, ct);
        if (!result.Success)
        {
            logger.AddKioskFailed(result.ErrorSummary);
            TempData["FlashError"] = result.ErrorSummary;
        }
        else
        {
            TempData["FlashSuccess"] = $"Kiosk '{vm.KioskCode}' provisioned.";
        }
        return RedirectToAction(nameof(KioskConfiguration));
    }

    /// <summary>Edit kiosk.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:write")]
    public async Task<IActionResult> EditKiosk(KioskMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await kioskService.EditAsync(vm, ct);
        TempData[result.Success ? "FlashSuccess" : "FlashError"]
            = result.Success ? $"Kiosk '{vm.KioskCode}' updated." : result.ErrorSummary;
        return RedirectToAction(nameof(KioskConfiguration));
    }

    /// <summary>Delete kiosk.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:admin")]
    public async Task<IActionResult> DeleteKiosk(string code, CancellationToken ct = default)
    {
        var result = await kioskService.DeleteAsync(code, ct);
        TempData[result.Success ? "FlashSuccess" : "FlashError"]
            = result.Success ? $"Kiosk '{code}' decommissioned." : result.ErrorSummary;
        return RedirectToAction(nameof(KioskConfiguration));
    }

    // ─────────────────────────── Screen savers ────────────────────────────────

    /// <summary>Screen saver configuration.</summary>
    [HttpGet]
    [Permission("kiosks:read")]
    public async Task<IActionResult> ScreenSaverConfiguration(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await screenSaverService.ListAsync(search, stat, pageSize, pageNo, ct);

        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add screen saver.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:write")]
    public async Task<IActionResult> AddScreenSaver(ScreenSaverMasterViewModel vm, IFormFile? photo, CancellationToken ct = default)
    {
        // Real file upload to blob storage comes in a later pass. For now, accept the URL field
        // (or generate a placeholder if a file was provided so the row at least lands).
        if (photo is not null && photo.Length > 0)
        {
            vm.ImgUrl ??= $"/uploads/screensavers/pending/{Guid.NewGuid()}-{Path.GetFileName(photo.FileName)}";
        }

        var result = await screenSaverService.AddAsync(vm, ct);
        TempData[result.Success ? "FlashSuccess" : "FlashError"]
            = result.Success ? $"Screen saver '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(ScreenSaverConfiguration));
    }

    /// <summary>Edit screen saver.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:write")]
    public async Task<IActionResult> EditScreenSaver(ScreenSaverMasterViewModel vm, IFormFile? photo, CancellationToken ct = default)
    {
        if (photo is not null && photo.Length > 0)
        {
            vm.ImgUrl ??= $"/uploads/screensavers/pending/{Guid.NewGuid()}-{Path.GetFileName(photo.FileName)}";
        }

        var result = await screenSaverService.EditAsync(vm, ct);
        TempData[result.Success ? "FlashSuccess" : "FlashError"]
            = result.Success ? $"Screen saver '{vm.Code}' updated." : result.ErrorSummary;
        return RedirectToAction(nameof(ScreenSaverConfiguration));
    }

    /// <summary>Delete screen saver.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("kiosks:admin")]
    public async Task<IActionResult> DeleteScreenSaver(string code, CancellationToken ct = default)
    {
        var result = await screenSaverService.DeleteAsync(code, ct);
        TempData[result.Success ? "FlashSuccess" : "FlashError"]
            = result.Success ? $"Screen saver '{code}' deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(ScreenSaverConfiguration));
    }
}
