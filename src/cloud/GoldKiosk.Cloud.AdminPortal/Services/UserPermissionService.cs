using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Reads the user's permission grants from identity.role_permissions joined
/// to identity.user_roles. Caches the set in HttpContext.Items so multiple
/// [Permission] checks during a single request hit memory.
/// </summary>
public sealed class UserPermissionService : IUserPermissionService
{
    private const string CacheKey = "uperm.grants";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IHttpContextAccessor _http;

    /// <summary>Initializes a new instance of the <see cref="UserPermissionService"/> class.</summary>
    public UserPermissionService(AppDbContext db, ICurrentUserService current, IHttpContextAccessor http)
    {
        _db = db;
        _current = current;
        _http = http;
    }

    /// <summary>Has.</summary>
    public async Task<bool> HasAsync(string permissionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var grants = await GetGrantsAsync(ct);
        return grants.Contains(permissionCode);
    }

    /// <summary>Get grants.</summary>
    public async Task<IReadOnlySet<string>> GetGrantsAsync(CancellationToken ct = default)
    {
        var ctx = _http.HttpContext;
        if (ctx is not null && ctx.Items.TryGetValue(CacheKey, out var cached) && cached is IReadOnlySet<string> set)
        {
            return set;
        }

        IReadOnlySet<string> grants;
        if (_current.UserId is not Guid userId)
        {
            grants = (IReadOnlySet<string>)new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            // Raw SQL: avoids us needing to map activation_keys / role_permissions
            // in EF. RLS is already pinned to the user's tenant by the
            // TenantContextInterceptor before this query runs.
            var rows = await _db.Database
                .SqlQueryRaw<string>(@"
                    SELECT p.code AS ""Value""
                      FROM identity.user_roles ur
                      JOIN identity.role_permissions rp ON rp.role_id = ur.role_id
                      JOIN identity.permissions     p  ON p.id       = rp.permission_id
                      JOIN identity.roles           r  ON r.id       = ur.role_id
                     WHERE ur.user_id    = {0}
                       AND ur.revoked_at IS NULL
                       AND (ur.expires_at IS NULL OR ur.expires_at > now())
                       AND r.is_active   = true
                       AND r.deleted_at  IS NULL",
                    userId)
                .ToListAsync(ct);

            grants = (IReadOnlySet<string>)new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
        }

        ctx?.Items[CacheKey] = grants;

        return grants;
    }
}
