using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Sales controller.</summary>
[Authorize]
public class SalesController(
    ISalesService salesService,
    ILogger<SalesController> logger) : Controller
{
    // Kiosk transaction kinds — must match the CHECK on tx.transactions.kind
    // (see db/0016_tables_tx.sql): precious_sale | pawn | voucher_redemption | …
    private const string KindPreciousSale = "precious_sale";
    private const string KindPawn = "pawn";

    /// <summary>Precious metal.</summary>
    [HttpGet]
    [Permission("sales:read")]
    public async Task<IActionResult> PreciousMetal(
        string? search,
        string? source,
        DateTime? from,
        DateTime? to,
        bool? showJunkOnly,
        bool? showActiveOnly,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var model = await salesService.ListAsync(
            KindPreciousSale, search, source, from, to,
            showJunkOnly ?? false, showActiveOnly ?? true,
            pageSize, pageNo, ct);

        PopulateViewBag(search, source, from, to, showJunkOnly, showActiveOnly, pageSize, pageNo);
        return View(model);
    }

    /// <summary>Pawn sales.</summary>
    [HttpGet]
    [Permission("sales:read")]
    public async Task<IActionResult> PawnSales(
        string? search,
        string? source,
        DateTime? from,
        DateTime? to,
        bool? showJunkOnly,
        bool? showActiveOnly,
        int pageSize = 10,
        int pageNo = 1,
        CancellationToken ct = default)
    {
        var model = await salesService.ListAsync(
            KindPawn, search, source, from, to,
            showJunkOnly ?? false, showActiveOnly ?? true,
            pageSize, pageNo, ct);

        PopulateViewBag(search, source, from, to, showJunkOnly, showActiveOnly, pageSize, pageNo);
        return View(model);
    }

    /// <summary>Export clients csv.</summary>
    [HttpGet]
    [Permission("sales:read")]
    public async Task<IActionResult> ExportClientsCsv(
        string kind,
        string? search,
        string? source,
        DateTime? from,
        DateTime? to,
        bool? showJunkOnly,
        bool? showActiveOnly,
        CancellationToken ct = default)
    {
        if (kind != KindPreciousSale && kind != KindPawn)
        {
            logger.ExportClientsCsvInvalidKind(kind);
            return BadRequest("kind must be 'precious_sale' or 'pawn'.");
        }

        var bytes = await salesService.ExportCsvAsync(
            kind, search, source, from, to,
            showJunkOnly ?? false, showActiveOnly ?? true, ct);

        var fileName = kind == KindPreciousSale ? "precious-metal-sales.csv" : "pawn-sales.csv";
        return File(bytes, "text/csv", fileName);
    }

    private void PopulateViewBag(
        string? search,
        string? source,
        DateTime? from,
        DateTime? to,
        bool? showJunkOnly,
        bool? showActiveOnly,
        int pageSize,
        int pageNo)
    {
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Source = source;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.ShowJunkOnly = showJunkOnly ?? false;
        ViewBag.ShowActiveOnly = showActiveOnly ?? true;
    }
}
