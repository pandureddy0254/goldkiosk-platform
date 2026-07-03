using GoldKiosk.Cloud.AdminPortal.Logging;
using System.Globalization;
using System.Text.Json;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Reports controller.</summary>
[Authorize]
public class ReportsController(
    IReportsService reportsService,
    ILogger<ReportsController> logger) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public IActionResult Index() => RedirectToAction(nameof(PreciousMetal));

    /// <summary>Precious metal.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> PreciousMetal(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.DailySalesAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.SaleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            kind = r.Kind,
            value = r.TotalPayout,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Holding sales.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> HoldingSales(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.HoldingSalesAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.Metal,
            holding = r.HoldingWeightG,
            sold = r.SoldWeightG,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Carat weight.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> CaratWeight(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.CaratWeightAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = $"{r.Metal} {r.BilledKarat}K",
            value = r.TotalWeightG,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Total expense.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> TotalExpense(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.TotalExpenseAsync(from, to, ct);
        if (vm.ExpenseTableMissing)
        {
            logger.TotalExpenseTableMissing();
        }

        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            value = r.Amount,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Worth.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> Worth(DateTime? asOf, CancellationToken ct = default)
    {
        var vm = await reportsService.WorthAsync(asOf, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.Metal,
            value = r.EstimatedWorth,
        }));
        ViewBag.AsOf = vm.AsOf;
        return View(vm);
    }

    /// <summary>Sales payout.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> SalesPayout(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.SalesPayoutAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows
            .GroupBy(r => r.OccurredOn.Date)
            .Select(g => new
            {
                label = g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                value = g.Sum(r => r.NetPayout),
            }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Expected profit.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> ExpectedProfit(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.ExpectedProfitAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.ProfitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            value = r.ExpectedProfit,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Profit after expense.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> ProfitAfterExpense(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.ProfitAfterExpenseAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(vm.Rows.Select(r => new
        {
            label = r.ProfitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            net = r.NetProfit,
            expense = r.Expenses,
        }));
        SetFilters(from, to);
        return View(vm);
    }

    /// <summary>Offer.</summary>
    [HttpGet]
    [Permission("reports:read")]
    public async Task<IActionResult> Offer(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vm = await reportsService.OffersAsync(from, to, ct);
        ViewBag.ChartData = JsonSerializer.Serialize(new
        {
            active = vm.ActiveCount,
            inactive = vm.InactiveCount,
        });
        SetFilters(from, to);
        return View(vm);
    }

    private void SetFilters(DateTime? from, DateTime? to)
    {
        ViewBag.From = from;
        ViewBag.To = to;
    }
}
