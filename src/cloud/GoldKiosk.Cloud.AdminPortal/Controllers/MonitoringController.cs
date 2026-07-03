using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Monitoring controller.</summary>
[Authorize]
public class MonitoringController(
    IExceptionMonitoringService exceptionService,
    IApiMonitoringService apiService,
    IUserActivityLogService activityService,
    IKioskInventoryMonitoringService inventoryService,
    ILogger<MonitoringController> logger) : Controller
{
    // ──────────────────────── Exception Monitoring ─────────────────────────

    /// <summary>Exception monitoring.</summary>
    [HttpGet]
    [Permission("monitoring:read")]
    public async Task<IActionResult> ExceptionMonitoring(
        string? search,
        string? severity,
        string? source,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var models = await exceptionService.ListAsync(search, severity, source, StartDate, EndDate, pageSize, pageNo, ct);

        ViewBag.SeverityDropdown = PortalHelpers.GetExceptionSeverityDropdown();
        ViewBag.SourceDropdown = PortalHelpers.GetExceptionSourceDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Severity = severity;
        ViewBag.Source = source;
        ViewBag.StartDate = StartDate;
        ViewBag.EndDate = EndDate;
        return View(models);
    }

    /// <summary>Resolve exception.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("monitoring:read")]
    public async Task<IActionResult> ResolveException(Guid id, CancellationToken ct = default)
    {
        var result = await exceptionService.MarkResolvedAsync(id, ct);
        if (!result.Success)
        {
            logger.ResolveExceptionFailed(result.ErrorSummary);
        }

        TempData[result.Success ? "ExceptionInfo" : "ExceptionError"]
            = result.Success ? "Exception marked as resolved." : result.ErrorSummary;
        return RedirectToAction(nameof(ExceptionMonitoring));
    }

    // ──────────────────────── API Health Monitoring ────────────────────────

    /// <summary>API health monitoring.</summary>
    [HttpGet]
    [Permission("monitoring:read")]
    public async Task<IActionResult> ApiHealthMonitoring(
        string? search,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var models = await apiService.GetStatsAsync(search, pageSize, pageNo, ct);

        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        return View(models);
    }

    // ─────────────────────────── User Activity ─────────────────────────────

    /// <summary>User activity log.</summary>
    [HttpGet]
    [Permission("monitoring:read")]
    public async Task<IActionResult> UserActivityLog(
        string? search,
        string? logType,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var models = await activityService.ListAsync(search, logType, StartDate, EndDate, pageSize, pageNo, ct);

        ViewBag.LogTypeDropdown = PortalHelpers.GetAuditLogTypeDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.LogType = logType;
        ViewBag.StartDate = StartDate;
        ViewBag.EndDate = EndDate;
        return View(models);
    }

    // ─────────────────────── Precious Metal (stub) ─────────────────────────

    /// <summary>Precious metal monitoring.</summary>
    [HttpGet]
    [Permission("monitoring:read")]
    public async Task<IActionResult> PreciousMetalMonitoring(
        string? search,
        string? metal,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var models = await inventoryService.ListAsync(search, metal, pageSize, pageNo, ct);

        ViewBag.MetalDropdown = PortalHelpers.GetMetalDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Metal = metal;
        return View(models);
    }
}
