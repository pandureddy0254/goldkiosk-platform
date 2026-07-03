using System.Diagnostics;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Home controller.</summary>
[Authorize]
public class HomeController(IDashboardService dashboardService) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("dashboard:read")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var vm = await dashboardService.GetAsync(ct);
        return View(vm);
    }

    /// <summary>Privacy.</summary>
    [HttpGet]
    [Permission("dashboard:read")]
    public IActionResult Privacy() => View();

    /// <summary>Error.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Permission("dashboard:read")]
    public IActionResult Error() =>
        View(new Viewmodel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
