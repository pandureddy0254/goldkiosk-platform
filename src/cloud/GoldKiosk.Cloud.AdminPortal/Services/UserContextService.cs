using System.Security.Claims;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// HttpContext-backed implementation. Caches results in
/// <c>HttpContext.Items</c> so repeated calls on the same request hit
/// memory, not the database. Scoped lifetime — never cache across requests.
/// </summary>
public sealed class UserContextService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor http) : IUserContextService
{
    private const string DisplayNameKey = "uctx.display_name";
    private const string InitialsKey = "uctx.initials";
    private const string RoleNameKey = "uctx.role_name";
    private const string TenantNameKey = "uctx.tenant_name";
    private const string TenantCodeKey = "uctx.tenant_code";
    private const string TenantRegionKey = "uctx.tenant_region";

    /// <summary>Get display name.</summary>
    public Task<string> GetDisplayNameAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(DisplayNameKey, out var cached) && cached is string s)
        {
            return Task.FromResult(s);
        }

        var principal = ctx?.User;
        var given = principal?.FindFirstValue(ClaimTypes.GivenName);
        var family = principal?.FindFirstValue(ClaimTypes.Surname);

        var name = string.Join(' ',
            new[] { given, family }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = principal?.FindFirstValue(ClaimTypes.Email)
                ?? principal?.Identity?.Name
                ?? "Guest";
        }

        ctx?.Items[DisplayNameKey] = name;

        return Task.FromResult(name);
    }

    /// <summary>Get primary role name.</summary>
    public async Task<string?> GetPrimaryRoleNameAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(RoleNameKey, out var cached))
        {
            return cached as string;
        }

        if (currentUser.UserId is not Guid userId)
        {
            ctx?.Items[RoleNameKey] = null;

            return null;
        }

        var roleName = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == userId
                && ur.RevokedAt == null
                && (ur.ExpiresAt == null || ur.ExpiresAt > DateTimeOffset.UtcNow)
                && r.IsActive
                && r.DeletedAt == null
            orderby ur.GrantedAt
            select r.Name
        ).FirstOrDefaultAsync(ct);

        ctx?.Items[RoleNameKey] = roleName;

        return roleName;
    }

    /// <summary>Get tenant legal name.</summary>
    public async Task<string?> GetTenantLegalNameAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(TenantNameKey, out var cached))
        {
            return cached as string;
        }

        if (currentUser.TenantId is not Guid tenantId)
        {
            ctx?.Items[TenantNameKey] = null;

            return null;
        }

        var legalName = await db.Database
            .SqlQueryRaw<string>(
                "SELECT legal_name AS \"Value\" FROM tenancy.tenants WHERE id = {0} LIMIT 1",
                tenantId)
            .FirstOrDefaultAsync(ct);

        ctx?.Items[TenantNameKey] = legalName;

        return legalName;
    }

    /// <summary>Get initials.</summary>
    public Task<string> GetInitialsAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(InitialsKey, out var cached) && cached is string s)
        {
            return Task.FromResult(s);
        }

        var principal = ctx?.User;
        var given = principal?.FindFirstValue(ClaimTypes.GivenName);
        var family = principal?.FindFirstValue(ClaimTypes.Surname);

        string initials;
        if (!string.IsNullOrWhiteSpace(given) && !string.IsNullOrWhiteSpace(family))
        {
            initials = string.Concat(char.ToUpperInvariant(given[0]), char.ToUpperInvariant(family[0]));
        }
        else
        {
            var email = principal?.FindFirstValue(ClaimTypes.Email) ?? "GK";
            initials = (email.Length >= 2 ? email[..2] : "GK").ToUpperInvariant();
        }

        ctx?.Items[InitialsKey] = initials;

        return Task.FromResult(initials);
    }

    /// <summary>Get tenant code.</summary>
    public async Task<string?> GetTenantCodeAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(TenantCodeKey, out var cached))
        {
            return cached as string;
        }

        if (currentUser.TenantId is not Guid tenantId)
        {
            ctx?.Items[TenantCodeKey] = null;

            return null;
        }

        var code = await db.Database
            .SqlQueryRaw<string>(
                "SELECT code AS \"Value\" FROM tenancy.tenants WHERE id = {0} LIMIT 1",
                tenantId)
            .FirstOrDefaultAsync(ct);

        ctx?.Items[TenantCodeKey] = code;

        return code;
    }

    /// <summary>Get tenant region.</summary>
    public async Task<string?> GetTenantRegionAsync(CancellationToken ct = default)
    {
        var ctx = http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(TenantRegionKey, out var cached))
        {
            return cached as string;
        }

        if (currentUser.TenantId is not Guid tenantId)
        {
            ctx?.Items[TenantRegionKey] = null;

            return null;
        }

        // Compose a human label from the joined tenant + country.
        var label = await db.Database
            .SqlQueryRaw<string>(@"
                SELECT (c.name || ' · primary · ' || t.status) AS ""Value""
                FROM tenancy.tenants t
                JOIN tenancy.countries c ON c.iso_code = t.home_country_code
                WHERE t.id = {0}
                LIMIT 1",
                tenantId)
            .FirstOrDefaultAsync(ct);

        ctx?.Items[TenantRegionKey] = label;

        return label;
    }
}
