using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Feedback controller.</summary>
[Authorize]
public class FeedbackController(
    IFeedbackService feedbackService,
    ILogger<FeedbackController> logger) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("feedback:read")]
    public async Task<IActionResult> Index(
        string? search,
        string? stat,
        int pageSize = 10,
        int pageNo = 1,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        CancellationToken ct = default)
    {
        var models = await feedbackService.ListAsync(search, stat, StartDate, EndDate, pageSize, pageNo, ct);

        ViewBag.StatusDropdown = PortalHelpers.GetFeedbackDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        ViewBag.StartDate = StartDate;
        ViewBag.EndDate = EndDate;
        return View(models);
    }

    /// <summary>Mark as read.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("feedback:write")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        var result = await feedbackService.MarkAsReadAsync(id, ct);
        TempData[result.Success ? "FeedbackInfo" : "FeedbackError"]
            = result.Success ? "Feedback marked as read." : result.ErrorSummary;
        if (!result.Success)
        {
            logger.MarkAsReadFailed(result.ErrorSummary);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Mark as unread.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("feedback:write")]
    public async Task<IActionResult> MarkAsUnread(Guid id, CancellationToken ct = default)
    {
        var result = await feedbackService.MarkAsUnreadAsync(id, ct);
        TempData[result.Success ? "FeedbackInfo" : "FeedbackError"]
            = result.Success ? "Feedback marked as unread." : result.ErrorSummary;
        if (!result.Success)
        {
            logger.MarkAsUnreadFailed(result.ErrorSummary);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Export csv.</summary>
    [HttpGet]
    [Permission("feedback:read")]
    public async Task<IActionResult> ExportCsv(
        string? search,
        string? stat,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        CancellationToken ct = default)
    {
        var bytes = await feedbackService.ExportCsvAsync(search, stat, StartDate, EndDate, ct);
        return File(bytes, "text/csv", "feedback.csv");
    }
}
