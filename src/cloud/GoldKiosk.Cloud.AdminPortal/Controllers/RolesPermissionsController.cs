using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Infrastructure.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>
/// Roles &amp; permissions editor (the Administration · Access control screen).
/// </summary>
/// <remarks>
/// This is the public, opinionated controller for managing role permissions —
/// the legacy <c>UserManagementController.AccessControl</c> matrix lives on
/// alongside it for the controller × action regression. Both ultimately mutate
/// <c>identity.role_permissions</c>.
/// </remarks>
[Authorize]
public sealed class RolesPermissionsController(
    IRolesPermissionsService rolesService,
    ICurrentUserService currentUser,
    ILogger<RolesPermissionsController> logger) : Controller
{
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("roles:read")]
    public async Task<IActionResult> Index(Guid? roleId, CancellationToken ct = default)
    {
        var model = await rolesService.GetIndexAsync(roleId, ct);
        return View(model);
    }

    /// <summary>Create custom role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> CreateCustomRole(
        string code,
        string name,
        string? description,
        IList<string>? permissionCodes,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid actor)
        {
            TempData["FlashError"] = "Session expired. Sign in again.";
            return RedirectToAction(nameof(Index));
        }

        var result = await rolesService.CreateCustomRoleAsync(
            code, name, description,
            permissionCodes ?? new List<string>(),
            actor, ct);

        if (result.Success)
        {
            TempData["FlashSuccess"] = $"Custom role '{name}' created.";
            return RedirectToAction(nameof(Index), new { roleId = result.Value });
        }

        logger.CreateCustomRoleRejected(result.ErrorSummary);
        TempData["FlashError"] = result.ErrorSummary;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Update permissions.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> UpdatePermissions(
        Guid roleId,
        IList<string>? permissionCodes,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid actor)
        {
            TempData["FlashError"] = "Session expired. Sign in again.";
            return RedirectToAction(nameof(Index));
        }

        var result = await rolesService.UpdatePermissionsAsync(
            roleId,
            permissionCodes ?? new List<string>(),
            actor, ct);

        if (result.Success)
        {
            TempData["FlashSuccess"] = "Permission grants saved.";
        }
        else
        {
            TempData["FlashError"] = result.ErrorSummary;
        }

        return RedirectToAction(nameof(Index), new { roleId });
    }

    /// <summary>Delete custom role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> DeleteCustomRole(Guid roleId, CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid actor)
        {
            TempData["FlashError"] = "Session expired. Sign in again.";
            return RedirectToAction(nameof(Index));
        }

        var result = await rolesService.DeleteCustomRoleAsync(roleId, actor, ct);

        TempData[result.Success ? "FlashSuccess" : "FlashError"] =
            result.Success ? "Custom role deleted." : result.ErrorSummary;

        return RedirectToAction(nameof(Index));
    }
}
