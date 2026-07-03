using System.Diagnostics;
using GoldKiosk.Cloud.CRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Dashboard landing page and the shared error endpoint.</summary>
public class HomeController : Controller
{
    /// <summary>Renders the dashboard.</summary>
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["ActiveSection"] = "dashboard";
        return View();
    }

    /// <summary>Shared error page (exception handler target).</summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
