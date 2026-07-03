using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Models.Invitations;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Cloud.AdminPortal.Services.Email;
using GoldKiosk.Cloud.AdminPortal.Services.Invitations;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>User management controller.</summary>
[Authorize]
public class UserManagementController(
    IRoleManagementService roleService,
    IUserAdministrationService userService,
    IAccessControlService accessControlService,
    IUserInvitationService invitations,
    ICurrentUserService currentUser,
    IEmailSender email,
    AppDbContext db,
    ILogger<UserManagementController> logger) : Controller
{

    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("users:read")]
    public IActionResult Index() => RedirectToAction(nameof(ProfileRoles));

    // ─── Profile & Roles (users) ───────────────────────────────────────────────
    /// <summary>Profile roles.</summary>
    [HttpGet]
    [Permission("users:read")]
    public async Task<IActionResult> ProfileRoles(string? search, string? role, string? status, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var model = await userService.ListAsync(search, role, status, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.RoleFilter = role;
        ViewBag.StatusFilter = status;
        return View(model);
    }

    /// <summary>Add user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:write")]
    public async Task<IActionResult> AddUser(UserRoleMapVM vm, CancellationToken ct = default)
    {
        var result = await userService.AddAsync(vm, ct);
        TempData[result.Success ? "UserInfo" : "UserError"]
            = result.Success ? "User created." : result.ErrorSummary;
        return RedirectToAction(nameof(ProfileRoles));
    }

    /// <summary>Edit user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:write")]
    public async Task<IActionResult> EditUser(UserRoleMapVM vm, CancellationToken ct = default)
    {
        var result = await userService.EditAsync(vm, ct);
        TempData[result.Success ? "UserInfo" : "UserError"]
            = result.Success ? "User updated." : result.ErrorSummary;
        return RedirectToAction(nameof(ProfileRoles));
    }

    /// <summary>Delete user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:admin")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct = default)
    {
        var result = await userService.DeleteAsync(userId, ct);
        TempData[result.Success ? "UserInfo" : "UserError"]
            = result.Success ? "User deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(ProfileRoles));
    }

    /// <summary>Assign role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:admin")]
    public async Task<IActionResult> AssignRole(Guid userId, string roleCode, CancellationToken ct = default)
    {
        var result = await userService.AssignRoleAsync(userId, roleCode, ct);
        TempData[result.Success ? "UserInfo" : "UserError"]
            = result.Success ? "Role assigned." : result.ErrorSummary;
        return RedirectToAction(nameof(ProfileRoles));
    }

    /// <summary>Remove role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:admin")]
    public async Task<IActionResult> RemoveRole(Guid userId, string roleCode, CancellationToken ct = default)
    {
        var result = await userService.RemoveRoleAsync(userId, roleCode, ct);
        TempData[result.Success ? "UserInfo" : "UserError"]
            = result.Success ? "Role removed." : result.ErrorSummary;
        return RedirectToAction(nameof(ProfileRoles));
    }

    // ─── Invite a user by email (creates a one-time token) ──────────────────────

    /// <summary>Invite.</summary>
    [HttpGet]
    [Permission("users:read")]
    public async Task<IActionResult> Invite(CancellationToken ct = default)
    {
        return View(await BuildInviteVmAsync(new InviteUserViewModel(), ct));
    }

    /// <summary>Invite.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:write")]
    public async Task<IActionResult> Invite(InviteUserViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid invitedBy || currentUser.TenantId is not Guid tenantId)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildInviteVmAsync(vm, ct));
        }

        try
        {
            var created = await invitations.CreateAsync(
                tenantId, invitedBy,
                vm.Email, vm.FirstName ?? "", vm.LastName ?? "",
                vm.RoleId, ct);

            var role = await db.Roles.AsNoTracking()
                .Where(r => r.Id == vm.RoleId)
                .Select(r => new { r.Name })
                .FirstOrDefaultAsync(ct);

            var inviteUrl = Url.Action("AcceptInvite", "Account",
                new { token = created.Token },
                Request.Scheme, Request.Host.Value)!;

            var fullName = $"{vm.FirstName} {vm.LastName}".Trim();
            await email.SendAsync(new EmailMessage
            {
                ToEmail = vm.Email,
                ToName = string.IsNullOrWhiteSpace(fullName) ? vm.Email : fullName,
                Subject = "You've been invited to Gold Kiosk",
                TextBody = BuildInviteEmailBody(fullName, role?.Name ?? "Member", inviteUrl, created.ExpiresAt),
            }, ct);

            TempData["UserInfo"] = $"Invitation sent to {vm.Email}. Token valid until {created.ExpiresAt:dd MMM yyyy}.";
            return RedirectToAction(nameof(ProfileRoles));
        }
        catch (Exception ex)
        {
            logger.InvitationSendFailed(ex);
            ModelState.AddModelError(string.Empty, "Could not create the invitation. Check the logs.");
            return View(await BuildInviteVmAsync(vm, ct));
        }
    }

    /// <summary>Revoke invite.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:write")]
    public async Task<IActionResult> RevokeInvite(Guid invitationId, CancellationToken ct = default)
    {
        await invitations.RevokeAsync(invitationId, "revoked by admin", ct);
        TempData["UserInfo"] = "Invitation revoked.";
        return RedirectToAction(nameof(ProfileRoles));
    }

    /// <summary>Resend invite.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("users:write")]
    public async Task<IActionResult> ResendInvite(Guid invitationId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Forbid();
        }

        try
        {
            var newToken = await invitations.RegenerateTokenAsync(invitationId, ct);

            // Look up the invite + role name to re-build the email.
            var pending = (await invitations.ListPendingAsync(tenantId, ct))
                .FirstOrDefault(p => p.Id == invitationId);

            if (pending is not null)
            {
                var inviteUrl = Url.Action("AcceptInvite", "Account",
                    new { token = newToken }, Request.Scheme, Request.Host.Value)!;
                await email.SendAsync(new EmailMessage
                {
                    ToEmail = pending.InviteeEmail,
                    ToName = string.IsNullOrWhiteSpace(pending.FullName) ? pending.InviteeEmail : pending.FullName,
                    Subject = "Your Gold Kiosk invitation (resent)",
                    TextBody = BuildInviteEmailBody(pending.FullName, pending.RoleName, inviteUrl, pending.ExpiresAt),
                }, ct);
            }
            TempData["UserInfo"] = "Invitation resent.";
        }
        catch (Exception ex)
        {
            logger.InvitationResendFailed(ex, invitationId);
            TempData["UserError"] = "Could not resend invitation.";
        }
        return RedirectToAction(nameof(ProfileRoles));
    }

    private async Task<InviteUserViewModel> BuildInviteVmAsync(InviteUserViewModel seed, CancellationToken ct)
    {
        if (currentUser.TenantId is Guid tenantId)
        {
            seed.AvailableRoles = await db.Roles.AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.IsActive && r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .Select(r => new RoleOption { Id = r.Id, Code = r.Code, Name = r.Name, Description = r.Description ?? "" })
                .ToListAsync(ct);
        }
        return seed;
    }

    private static string BuildInviteEmailBody(string toName, string roleName, string inviteUrl, DateTimeOffset expiresAt) =>
        $"""
        Hi {(string.IsNullOrWhiteSpace(toName) ? "there" : toName)},

        You've been invited to join Gold Kiosk as {roleName}.

        Click here to set up your account:
        {inviteUrl}

        This invitation expires on {expiresAt:dd MMM yyyy HH:mm} UTC. If it expires,
        ask your administrator to resend it.

        — Gold Kiosk
        """;

    // ─── Role master (CRUD) ────────────────────────────────────────────────────
    /// <summary>Role master.</summary>
    [HttpGet]
    [Permission("users:read")]
    public async Task<IActionResult> RoleMaster(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
    {
        var models = await roleService.ListAsync(search, stat, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.PageSize = pageSize;
        ViewBag.PageNo = pageNo;
        ViewBag.SearchTerm = search;
        ViewBag.Stat = stat;
        return View(models);
    }

    // Alias so URLs like /usermanagement/rolemanagement (used in the view's form action) resolve.
    /// <summary>Role management.</summary>
    [HttpGet]
    [Permission("users:read")]
    public Task<IActionResult> RoleManagement(string? search, string? stat, int pageSize = 10, int pageNo = 1, CancellationToken ct = default)
        => RoleMaster(search, stat, pageSize, pageNo, ct);

    /// <summary>Add role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> AddRole(RoleMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await roleService.AddAsync(vm, ct);
        TempData[result.Success ? "RoleInfo" : "RoleError"]
            = result.Success ? "Role created." : result.ErrorSummary;
        return RedirectToAction(nameof(RoleMaster));
    }

    /// <summary>Edit role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> EditRole(RoleMasterViewModel vm, CancellationToken ct = default)
    {
        var result = await roleService.EditAsync(vm, ct);
        TempData[result.Success ? "RoleInfo" : "RoleError"]
            = result.Success ? "Role updated." : result.ErrorSummary;
        return RedirectToAction(nameof(RoleMaster));
    }

    /// <summary>Delete role.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> DeleteRole(string code, CancellationToken ct = default)
    {
        var result = await roleService.DeleteAsync(code, ct);
        TempData[result.Success ? "RoleInfo" : "RoleError"]
            = result.Success ? "Role deleted." : result.ErrorSummary;
        return RedirectToAction(nameof(RoleMaster));
    }

    // ─── Access control (role × module permission matrix) ──────────────────────
    /// <summary>Access control.</summary>
    [HttpGet]
    [Permission("users:read")]
    public async Task<IActionResult> AccessControl(string? role, string? search, int pageSize = 25, int pageNo = 1, CancellationToken ct = default)
    {
        var model = await accessControlService.ListAsync(role, search, pageSize, pageNo, ct);
        ViewBag.StatusDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.RoleDropdown = PortalHelpers.GetIsActiveDropdown();
        ViewBag.SearchTerm = search;
        ViewBag.RoleFilter = role;
        return View(model);
    }

    /// <summary>Update access control.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> UpdateAccessControl(AccessControlPageViewModel vm, CancellationToken ct = default)
    {
        var result = await accessControlService.UpdateAsync(vm, ct);
        TempData[result.Success ? "AccessInfo" : "AccessError"]
            = result.Success ? "Access control updated." : result.ErrorSummary;
        return RedirectToAction(nameof(AccessControl), new { role = vm.RoleCode });
    }

    /// <summary>Update access control row.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("roles:write")]
    public async Task<IActionResult> UpdateAccessControlRow(AccessModuleViewModel vm, CancellationToken ct = default)
    {
        var result = await accessControlService.UpdateSingleAsync(vm, ct);
        TempData[result.Success ? "AccessInfo" : "AccessError"]
            = result.Success ? "Permission updated." : result.ErrorSummary;
        return RedirectToAction(nameof(AccessControl), new { role = vm.RoleCode });
    }
}
