using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Identity;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>User administration service.</summary>
public sealed class UserAdministrationService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher<AppUser> passwordHasher) : IUserAdministrationService
{
    /// <summary>List.</summary>
    public async Task<UserRoleMapVM> ListAsync(string? search, string? roleCode, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var usersQ = db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            usersQ = usersQ.Where(u =>
                EF.Functions.ILike(u.Email!, s) ||
                EF.Functions.ILike(u.FirstName, s) ||
                EF.Functions.ILike(u.LastName, s) ||
                (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            usersQ = usersQ.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var matchingRoleIds = db.Roles.AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.Code == roleCode && r.DeletedAt == null)
                .Select(r => r.Id);

            var userIdsWithRole = db.UserRoles.AsNoTracking()
                .Where(ur => ur.RevokedAt == null && matchingRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            usersQ = usersQ.Where(u => userIdsWithRole.Contains(u.Id));
        }

        var totals = await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.DeletedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(u => u.Status == "active"),
                InActive = g.Count(u => u.Status != "active")
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await usersQ.CountAsync(ct);

        var pageUsers = await usersQ
            .OrderByDescending(u => u.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                Mobile = u.PhoneNumber,
                u.Status,
                u.CreatedAt
            })
            .ToListAsync(ct);

        var pageUserIds = pageUsers.Select(u => u.Id).ToList();

        // Single round-trip for role assignments of the page slice.
        var assignments = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where pageUserIds.Contains(ur.UserId)
                  && ur.RevokedAt == null
                  && r.TenantId == tenantId
                  && r.DeletedAt == null
            select new { ur.UserId, RoleCode = r.Code, RoleName = r.Name }
        ).ToListAsync(ct);

        var assignmentsByUser = assignments
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => new UserRoleMapVM
            {
                RoleCode = x.RoleCode,
                RoleName = x.RoleName,
            }).ToList());

        var users = pageUsers.Select(u => new UserRoleMapVM
        {
            UserId = u.Id,
            UserCode = u.Id.ToString(),
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Mobile = u.Mobile ?? string.Empty,
            Status = u.Status,
            IsActive = string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase),
            CreatedOn = u.CreatedAt.UtcDateTime,
            AssignedRoles = assignmentsByUser.TryGetValue(u.Id, out var roles) ? roles : new List<UserRoleMapVM>(),
        }).ToList();

        var roleOptions = await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null && r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new UserRoleMapVM { RoleCode = r.Code, RoleName = r.Name })
            .ToListAsync(ct);

        return new UserRoleMapVM
        {
            Users = users,
            RoleOptions = roleOptions,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get by id.</summary>
    public async Task<UserRoleMapVM?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var u = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == userId && x.DeletedAt == null, ct);
        if (u is null)
        {
            return null;
        }

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == userId
                  && ur.RevokedAt == null
                  && r.TenantId == tenantId
                  && r.DeletedAt == null
            select new UserRoleMapVM { RoleCode = r.Code, RoleName = r.Name }
        ).ToListAsync(ct);

        return new UserRoleMapVM
        {
            UserId = u.Id,
            UserCode = u.Id.ToString(),
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Mobile = u.PhoneNumber ?? string.Empty,
            Status = u.Status,
            IsActive = string.Equals(u.Status, "active", StringComparison.OrdinalIgnoreCase),
            CreatedOn = u.CreatedAt.UtcDateTime,
            AssignedRoles = roles,
        };
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(UserRoleMapVM vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Email))
        {
            return OperationResult.Fail("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.FirstName) || string.IsNullOrWhiteSpace(vm.LastName))
        {
            return OperationResult.Fail("First and last name are required.");
        }

        var normalisedEmail = vm.Email.Trim().ToUpperInvariant();
        var duplicate = await db.Users.AnyAsync(u =>
            u.TenantId == tenantId && u.NormalizedEmail == normalisedEmail && u.DeletedAt == null, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"User with email '{vm.Email}' already exists.");
        }

        var status = string.IsNullOrWhiteSpace(vm.Status) ? "invited" : vm.Status.Trim().ToLowerInvariant();
        if (status is not ("invited" or "active" or "suspended" or "locked"))
        {
            return OperationResult.Fail("Invalid status value.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = vm.Email.Trim(),
            NormalizedUserName = normalisedEmail,
            Email = vm.Email.Trim(),
            NormalizedEmail = normalisedEmail,
            EmailConfirmed = false,
            PhoneNumber = string.IsNullOrWhiteSpace(vm.Mobile) ? null : vm.Mobile.Trim(),
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            Status = status,
            OtpMode = "off",
            Locale = "en",
            PasswordResetRequired = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Issue a strong random password; user will be required to reset on first login.
        user.PasswordHash = passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N") + "Aa1!");

        db.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(vm.RoleCode))
        {
            var role = await db.Roles.FirstOrDefaultAsync(r =>
                r.TenantId == tenantId && r.Code == vm.RoleCode && r.DeletedAt == null, ct);
            if (role is not null)
            {
                db.UserRoles.Add(new AppUserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    GrantedAt = DateTimeOffset.UtcNow,
                    GrantedByUserId = currentUser.UserId,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(UserRoleMapVM vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.UserId == Guid.Empty)
        {
            return OperationResult.Fail("User id is required.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.TenantId == tenantId && u.Id == vm.UserId && u.DeletedAt == null, ct);
        if (user is null)
        {
            return OperationResult.Fail("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.FirstName))
        {
            user.FirstName = vm.FirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(vm.LastName))
        {
            user.LastName = vm.LastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(vm.Mobile))
        {
            user.PhoneNumber = vm.Mobile.Trim();
        }

        if (!string.IsNullOrWhiteSpace(vm.Status))
        {
            var status = vm.Status.Trim().ToLowerInvariant();
            if (status is "invited" or "active" or "suspended" or "locked")
            {
                user.Status = status;
            }
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.TenantId == tenantId && u.Id == userId && u.DeletedAt == null, ct);
        if (user is null)
        {
            return OperationResult.Fail("User not found.");
        }

        user.DeletedAt = DateTimeOffset.UtcNow;
        user.Status = "suspended";
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Assign role.</summary>
    public async Task<OperationResult> AssignRoleAsync(Guid userId, string roleCode, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return OperationResult.Fail("Role code is required.");
        }

        var userExists = await db.Users.AnyAsync(u =>
            u.TenantId == tenantId && u.Id == userId && u.DeletedAt == null, ct);
        if (!userExists)
        {
            return OperationResult.Fail("User not found.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(r =>
            r.TenantId == tenantId && r.Code == roleCode && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{roleCode}' not found.");
        }

        var existing = await db.UserRoles.FirstOrDefaultAsync(ur =>
            ur.UserId == userId && ur.RoleId == role.Id && ur.RevokedAt == null, ct);
        if (existing is not null)
        {
            return OperationResult.Fail("Role already assigned.");
        }

        db.UserRoles.Add(new AppUserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = currentUser.UserId,
        });

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Remove role.</summary>
    public async Task<OperationResult> RemoveRoleAsync(Guid userId, string roleCode, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return OperationResult.Fail("Role code is required.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(r =>
            r.TenantId == tenantId && r.Code == roleCode && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{roleCode}' not found.");
        }

        var assignment = await db.UserRoles.FirstOrDefaultAsync(ur =>
            ur.UserId == userId && ur.RoleId == role.Id && ur.RevokedAt == null, ct);
        if (assignment is null)
        {
            return OperationResult.Fail("Role assignment not found.");
        }

        assignment.RevokedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static UserRoleMapVM EmptyResult(int pageSize, int pageNo) => new()
    {
        Users = new List<UserRoleMapVM>(),
        RoleOptions = new List<UserRoleMapVM>(),
        Total = 0,
        Active = 0,
        InActive = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        TotalPage = 0,
    };
}
