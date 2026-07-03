using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Operations controller.</summary>
[Authorize]
public class OperationsController(
    IDeploymentTicketService deploymentTickets,
    IMaintenanceTicketService maintenanceTickets,
    ITechnicianService technicians,
    ICollectionService collection,
    ILocationConfigurationService locations,
    IKioskSecurityTokenService securityTokens,
    ISystemHealthService systemHealth,
    IKioskInventoryMonitoringService inventory,
    ILogger<OperationsController> logger) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public IActionResult Index() => RedirectToAction(nameof(DeploymentTickets));

    // ───────────────────────── Deployment tickets ────────────────────────────

    /// <summary>Deployment tickets.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> DeploymentTickets(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await deploymentTickets.ListAsync(search, stat, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetTicketStatusDropdown();
        ViewBag.PriorityDropdown = PortalHelpers.GetTicketPriorityDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add deployment ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddDeploymentTicket(DeploymentTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await deploymentTickets.AddAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Deployment ticket '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(DeploymentTickets));
    }

    /// <summary>Edit deployment ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditDeploymentTicket(DeploymentTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await deploymentTickets.EditAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Deployment ticket updated." : result.ErrorSummary;
        return RedirectToAction(nameof(DeploymentTickets));
    }

    /// <summary>Delete deployment ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> DeleteDeploymentTicket(Guid id, CancellationToken ct = default)
    {
        var result = await deploymentTickets.DeleteAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Deployment ticket deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(DeploymentTickets));
    }

    /// <summary>Approve deployment ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> ApproveDeploymentTicket(Guid id, string? remarks, CancellationToken ct = default)
    {
        var result = await deploymentTickets.ChangeStatusAsync(id, "resolved", remarks, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Deployment ticket approved (resolved)." : result.ErrorSummary;
        return RedirectToAction(nameof(DeploymentTickets));
    }

    /// <summary>Reject deployment ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> RejectDeploymentTicket(Guid id, string? remarks, CancellationToken ct = default)
    {
        var result = await deploymentTickets.ChangeStatusAsync(id, "closed", remarks, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Deployment ticket closed." : result.ErrorSummary;
        return RedirectToAction(nameof(DeploymentTickets));
    }

    // ───────────────────────── Maintenance tickets ───────────────────────────

    /// <summary>Maintenance tickets.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> MaintenanceTickets(string? search, string? stat, string? ticketType, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await maintenanceTickets.ListAsync(search, stat, ticketType, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetTicketStatusDropdown();
        ViewBag.PriorityDropdown = PortalHelpers.GetTicketPriorityDropdown();
        ViewBag.TypeDropdown = PortalHelpers.GetMaintenanceTypeDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        ViewBag.TicketType = ticketType;
        return View(models);
    }

    /// <summary>Add maintenance ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddMaintenanceTicket(MaintenanceTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await maintenanceTickets.AddAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Maintenance ticket '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(MaintenanceTickets));
    }

    /// <summary>Edit maintenance ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditMaintenanceTicket(MaintenanceTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await maintenanceTickets.EditAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Maintenance ticket updated." : result.ErrorSummary;
        return RedirectToAction(nameof(MaintenanceTickets));
    }

    /// <summary>Delete maintenance ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> DeleteMaintenanceTicket(Guid id, CancellationToken ct = default)
    {
        var result = await maintenanceTickets.DeleteAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Maintenance ticket deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(MaintenanceTickets));
    }

    /// <summary>Change maintenance status.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> ChangeMaintenanceStatus(Guid id, string newStatus, string? remarks, CancellationToken ct = default)
    {
        var result = await maintenanceTickets.ChangeStatusAsync(id, newStatus, remarks, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Status changed to '{newStatus}'." : result.ErrorSummary;
        return RedirectToAction(nameof(MaintenanceTickets));
    }

    // ───────────────────────── Technicians ───────────────────────────────────

    /// <summary>Technician management.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> TechnicianManagement(string? search, Guid? clusterId, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await technicians.ListAsync(search, clusterId, stat, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetTechnicianStatusDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.ClusterId = clusterId;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add technician.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddTechnician(TechnicianViewModel vm, CancellationToken ct = default)
    {
        var result = await technicians.AddAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Technician '{vm.Name}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(TechnicianManagement));
    }

    /// <summary>Edit technician.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditTechnician(TechnicianViewModel vm, CancellationToken ct = default)
    {
        var result = await technicians.EditAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Technician updated." : result.ErrorSummary;
        return RedirectToAction(nameof(TechnicianManagement));
    }

    /// <summary>Delete technician.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> DeleteTechnician(Guid id, CancellationToken ct = default)
    {
        var result = await technicians.DeleteAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Technician deactivated." : result.ErrorSummary;
        return RedirectToAction(nameof(TechnicianManagement));
    }

    // ───────────────────────── Collection ────────────────────────────────────

    /// <summary>Collection tickets.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> CollectionTickets(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await collection.ListAsync(search, stat, pageSize, pageNo, ct);
        ViewBag.RunStatusDropdown = PortalHelpers.GetCollectionRunStatusDropdown();
        ViewBag.TicketStatusDropdown = PortalHelpers.GetCollectionTicketStatusDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add collection run.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddCollectionRun(CollectionRunViewModel vm, CancellationToken ct = default)
    {
        var result = await collection.AddRunAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Collection run '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    /// <summary>Edit collection run.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditCollectionRun(CollectionRunViewModel vm, CancellationToken ct = default)
    {
        var result = await collection.EditRunAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Collection run updated." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    /// <summary>Delete collection run.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> DeleteCollectionRun(Guid id, CancellationToken ct = default)
    {
        var result = await collection.DeleteRunAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Collection run deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    /// <summary>Add collection ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddCollectionTicket(CollectionTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await collection.AddTicketAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Collection ticket created." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    /// <summary>Edit collection ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditCollectionTicket(CollectionTicketViewModel vm, CancellationToken ct = default)
    {
        var result = await collection.EditTicketAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Collection ticket updated." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    /// <summary>Sign off collection ticket.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> SignOffCollectionTicket(Guid id, CancellationToken ct = default)
    {
        var result = await collection.SignOffAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Collection ticket signed off." : result.ErrorSummary;
        return RedirectToAction(nameof(CollectionTickets));
    }

    // ───────────────────────── Precious metal monitoring ─────────────────────

    /// <summary>Precious metal monitoring.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> PreciousMetalMonitoring(string? search, string? metal, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await inventory.ListAsync(search, metal, pageSize, pageNo, ct);
        ViewBag.MetalDropdown = PortalHelpers.GetMetalDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Metal = metal;
        return View(models);
    }

    // ───────────────────────── Location configuration ────────────────────────

    /// <summary>Location configuration.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> LocationConfiguration(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await locations.ListAsync(search, stat, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    /// <summary>Add location.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> AddLocation(KioskLocationViewModel vm, CancellationToken ct = default)
    {
        var result = await locations.AddAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? $"Location '{vm.Code}' created." : result.ErrorSummary;
        return RedirectToAction(nameof(LocationConfiguration));
    }

    /// <summary>Edit location.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> EditLocation(KioskLocationViewModel vm, CancellationToken ct = default)
    {
        var result = await locations.EditAsync(vm, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Location updated." : result.ErrorSummary;
        return RedirectToAction(nameof(LocationConfiguration));
    }

    /// <summary>Delete location.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken ct = default)
    {
        var result = await locations.DeleteAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Location deactivated." : result.ErrorSummary;
        return RedirectToAction(nameof(LocationConfiguration));
    }

    // ───────────────────────── Kiosk security tokens ─────────────────────────

    /// <summary>Kiosk security token.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> KioskSecurityToken(Guid? kioskId, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await securityTokens.ListAsync(kioskId, pageSize, pageNo, ct);
        // If a raw token was just minted, surface it via TempData (single-shot).
        if (TempData["NewlyIssuedRawToken"] is string raw)
        {
            models.NewlyIssuedRawToken = raw;
            if (TempData["NewlyIssuedKioskId"] is string kid && Guid.TryParse(kid, out var g))
            {
                models.NewlyIssuedKioskId = g;
            }
        }
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.KioskId = kioskId;
        return View(models);
    }

    /// <summary>Issue kiosk security token.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> IssueKioskSecurityToken(Guid kioskId, int? validityDays, CancellationToken ct = default)
    {
        if (kioskId == default)
        {
            TempData["OpsError"] = "Kiosk is required to issue a token.";
            return RedirectToAction(nameof(KioskSecurityToken));
        }

        var validity = validityDays is > 0 ? TimeSpan.FromDays(validityDays.Value) : (TimeSpan?)null;
        var result = await securityTokens.IssueAsync(kioskId, validity, ct);
        if (!string.IsNullOrEmpty(result.NewlyIssuedRawToken))
        {
            TempData["NewlyIssuedRawToken"] = result.NewlyIssuedRawToken;
            TempData["NewlyIssuedKioskId"] = kioskId.ToString();
            TempData["OpsInfo"] = "Token issued. Copy the raw token now — it cannot be retrieved later.";
        }
        else
        {
            TempData["OpsError"] = "Failed to issue token.";
            logger.IssueKioskSecurityTokenFailed(kioskId);
        }
        return RedirectToAction(nameof(KioskSecurityToken), new { kioskId });
    }

    /// <summary>Revoke kiosk security token.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("operations:write")]
    public async Task<IActionResult> RevokeKioskSecurityToken(Guid id, Guid? kioskId, CancellationToken ct = default)
    {
        var result = await securityTokens.RevokeAsync(id, ct);
        TempData[result.Success ? "OpsInfo" : "OpsError"]
            = result.Success ? "Token revoked." : result.ErrorSummary;
        return RedirectToAction(nameof(KioskSecurityToken), new { kioskId });
    }

    // ───────────────────────── System health check ───────────────────────────

    /// <summary>System health check.</summary>
    [HttpGet]
    [Permission("operations:read")]
    public async Task<IActionResult> SystemHealthCheck(CancellationToken ct = default)
    {
        var models = await systemHealth.GetDashboardAsync(ct);
        return View(models);
    }
}
