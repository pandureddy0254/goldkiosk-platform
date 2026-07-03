using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Audit logs controller.</summary>
[Authorize]
public class AuditLogsController(
    IAuditLogService auditLogService) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("audit:read")]
    public async Task<IActionResult> Index(
        string? search,
        string? feature,
        string? type,
        string? Status,
        string? category,
        string? subCategory,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var models = await auditLogService.ListAsync(
            search, feature, type, Status, category, subCategory,
            StartDate, EndDate, pageSize, pageNo, ct);

        ViewBag.StatusDropdown = PortalHelpers.GetSupportStatusDropdown();
        ViewBag.CategoryDropdown = PortalHelpers.GetSupportStatusDropdown();
        ViewBag.SubCategoryDropdown = PortalHelpers.GetSupportStatusDropdown();

        ViewBag.Search = search;
        ViewBag.SearchTerm = search;
        ViewBag.Status = Status;
        ViewBag.Category = category;
        ViewBag.SubCategory = subCategory;
        ViewBag.Feature = feature;
        ViewBag.Type = type;
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.StartDate = StartDate;
        ViewBag.EndDate = EndDate;

        return View(models);
    }

    /// <summary>Export csv.</summary>
    [HttpGet]
    [Permission("audit:export")]
    public async Task<IActionResult> ExportCsv(
        string? search,
        string? feature,
        string? type,
        string? Status,
        string? category,
        string? subCategory,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        CancellationToken ct = default)
    {
        var bytes = await auditLogService.ExportCsvAsync(
            search, feature, type, Status, category, subCategory,
            StartDate, EndDate, ct);
        return File(bytes, "text/csv", "audit-log.csv");
    }
}
