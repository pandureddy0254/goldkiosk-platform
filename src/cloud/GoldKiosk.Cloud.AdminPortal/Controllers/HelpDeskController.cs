using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Help desk controller.</summary>
[Authorize]
public class HelpDeskController(
    ISupportTicketService ticketService,
    ISosService sosService,
    ILogger<HelpDeskController> logger) : Controller
{
    // ───────────────────────────── Tickets ────────────────────────────────────

    /// <summary>Ticket.</summary>
    [HttpGet]
    [Permission("helpdesk:read")]
    public async Task<IActionResult> Ticket(
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
        var models = await ticketService.ListAsync(
            search, feature, type, Status, category, subCategory,
            StartDate, EndDate, pageSize, pageNo, ct);

        var categories = await ticketService.GetCategoriesAsync(ct);

        ViewBag.StatusDropdown = PortalHelpers.GetSupportStatusDropdown();
        ViewBag.CategoryDropdown = categories;
        ViewBag.SubCategoryDropdown = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        ViewBag.Search = search;
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

    /// <summary>Add support ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("helpdesk:write")]
    public async Task<IActionResult> AddSupportTicket(SupportTicketViewModel vm, IFormFile? documents, CancellationToken ct = default)
    {
        string? documentsUri = null;
        if (documents is not null && documents.Length > 0)
        {
            // Real blob upload is out of scope for this pass — store a placeholder URI
            // that downstream blob-migration can rewrite once the actual upload is wired.
            documentsUri = $"/uploads/helpdesk/pending/{Guid.NewGuid()}-{Path.GetFileName(documents.FileName)}";
        }

        var result = await ticketService.AddAsync(vm, documentsUri, ct);
        if (!result.Success)
        {
            logger.AddSupportTicketFailed(result.ErrorSummary);
            TempData["TicketError"] = result.ErrorSummary;
        }
        else
        {
            TempData["TicketInfo"] = "Ticket raised.";
        }
        return RedirectToAction(nameof(Ticket));
    }

    /// <summary>Get subcategories.</summary>
    [HttpGet]
    [Permission("helpdesk:read")]
    public async Task<JsonResult> GetSubcategories(string category, CancellationToken ct = default)
    {
        var items = await ticketService.GetSubcategoriesAsync(category, ct);
        // Match the contract the existing view JS expects: [ { code, text }, ... ]
        return Json(items.Select(x => new { code = x.Code, text = x.Text }));
    }

    // ─────────────────────────────── SOS ──────────────────────────────────────

    /// <summary>SOS.</summary>
    [HttpGet]
    [Permission("helpdesk:read")]
    public async Task<IActionResult> SOS(string? status, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await sosService.ListAsync(status, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetSosStatusDropdown();
        ViewBag.PriorityDropdown = PortalHelpers.GetSosPriorityDropdown();
        ViewBag.Status = status;
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        return View(models);
    }

    /// <summary>Acknowledge SOS.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("sos:write")]
    public async Task<IActionResult> AcknowledgeSos(Guid id, CancellationToken ct = default)
    {
        var result = await sosService.AcknowledgeAsync(id, ct);
        TempData[result.Success ? "SosInfo" : "SosError"]
            = result.Success ? "SOS acknowledged." : result.ErrorSummary;
        return RedirectToAction(nameof(SOS));
    }

    /// <summary>Dispatch SOS.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("sos:write")]
    public async Task<IActionResult> DispatchSos(Guid id, Guid technicianId, CancellationToken ct = default)
    {
        var result = await sosService.DispatchAsync(id, technicianId, ct);
        TempData[result.Success ? "SosInfo" : "SosError"]
            = result.Success ? "SOS dispatched." : result.ErrorSummary;
        return RedirectToAction(nameof(SOS));
    }

    /// <summary>Resolve SOS.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("sos:write")]
    public async Task<IActionResult> ResolveSos(Guid id, CancellationToken ct = default)
    {
        var result = await sosService.ResolveAsync(id, ct);
        TempData[result.Success ? "SosInfo" : "SosError"]
            = result.Success ? "SOS resolved." : result.ErrorSummary;
        return RedirectToAction(nameof(SOS));
    }
}
