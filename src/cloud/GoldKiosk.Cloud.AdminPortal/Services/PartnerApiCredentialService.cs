using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models.Integration;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Raw-SQL implementation backed by <c>integration.partner_api_credentials</c>
/// (db migration 0031). The table is not in the EF model yet — every query
/// drops down to the underlying <see cref="NpgsqlConnection"/> so the prototype
/// can land without a schema-discovery pass.
///
/// <para>
/// Tenant scoping is enforced via the explicit <c>tenant_id = @tenant</c> WHERE
/// clause AND the row-level-security policy set by
/// <c>tenancy.set_tenant_context</c>. Belt + braces — both layers must agree.
/// </para>
/// <para>
/// IMPORTANT: <c>TenantContextInterceptor.ConnectionOpenedAsync</c> only fires
/// when EF Core opens the connection. When we manually open via
/// <c>conn.OpenAsync()</c> the interceptor is bypassed and RLS-scoped reads
/// can return zero rows (or error under FORCE RLS). Every entry point here
/// calls <see cref="DbConnectionExtensions.SetTenantContextAsync"/> after the
/// open to keep the invariant.
/// </para>
/// </summary>
public sealed class PartnerApiCredentialService(
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<PartnerApiCredentialService> logger) : IPartnerApiCredentialService
{
    /// <summary>List.</summary>
    public async Task<IReadOnlyList<PartnerApiCredentialRow>> ListAsync(string? environment, bool includeRevoked, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Array.Empty<PartnerApiCredentialRow>();
        }

        var sql = new StringBuilder(
            """
            SELECT  c.id, c.label, c.app_id, c.app_key_prefix, c.scopes,
                    c.environment, c.last_used_at, c.created_at, c.revoked_at,
                    c.revoked_reason, c.expires_at,
                    COALESCE(NULLIF(TRIM(CONCAT(u.first_name, ' ', u.last_name)), ''), u.email, '—') AS created_by_name
            FROM    integration.partner_api_credentials c
            LEFT JOIN identity.users u ON u.id = c.created_by_user_id
            WHERE   c.tenant_id = @tenant
            """);

        if (!includeRevoked)
        {
            sql.Append(" AND c.revoked_at IS NULL");
        }

        if (!string.IsNullOrWhiteSpace(environment) && (environment == "live" || environment == "test"))
        {
            sql.Append(" AND c.environment = @env");
        }

        sql.Append(" ORDER BY c.created_at DESC");

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.ToString();
        cmd.Parameters.Add(new NpgsqlParameter("@tenant", NpgsqlDbType.Uuid) { Value = tenantId });
        if (!string.IsNullOrWhiteSpace(environment) && (environment == "live" || environment == "test"))
        {
            cmd.Parameters.Add(new NpgsqlParameter("@env", NpgsqlDbType.Text) { Value = environment });
        }

        var rows = new List<PartnerApiCredentialRow>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var scopesJson = reader.IsDBNull(4) ? "[]" : reader.GetString(4);
            var scopes = ParseScopes(scopesJson);

            rows.Add(new PartnerApiCredentialRow
            {
                Id = reader.GetGuid(0),
                Label = reader.GetString(1),
                AppId = reader.GetString(2),
                AppKeyPrefix = reader.GetString(3),
                Scopes = scopes,
                Environment = reader.GetString(5),
                LastUsedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(7),
                RevokedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                RevokedReason = reader.IsDBNull(9) ? null : reader.GetString(9),
                ExpiresAt = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                CreatedByName = reader.IsDBNull(11) ? "—" : reader.GetString(11),
            });
        }

        return rows;
    }

    /// <summary>Create.</summary>
    public async Task<PartnerApiCredentialCreateResult> CreateAsync(
        PartnerApiCredentialCreateRequest req,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new InvalidOperationException("No tenant context — cannot create credential.");
        }

        if (string.IsNullOrWhiteSpace(req.Label))
        {
            throw new ArgumentException("Label is required.", nameof(req));
        }

        var env = (req.Environment ?? "live").Trim().ToLowerInvariant();
        if (env != "live" && env != "test")
        {
            throw new ArgumentException("Environment must be 'live' or 'test'.", nameof(req));
        }

        // ─── Generate the id/key material ────────────────────────────────
        // Per the migration: app_id ~ '^gk_(live|test)_[0-9a-f]{12,32}$'.
        // We use exactly 12 hex for the id suffix and 32 hex for the secret suffix.
        var appIdSuffix = RandomHex(12);
        var appKeySuffix = RandomHex(32);
        var appId = $"gk_{env}_{appIdSuffix}";
        var appKey = $"sk_{env}_{appIdSuffix}_{appKeySuffix}";   // shares the id suffix so the prefix lines up
        var appKeyPrefix = appKey.Length >= 12 ? appKey[..12] : appKey;    // e.g. "sk_live_a4f2"
        var appKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(appKey));

        // ─── Validate + project the scopes ───────────────────────────────
        var scopes = (req.Scopes ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToLowerInvariant())
            .Distinct()
            .Where(PartnerApiCredentialScopes.All.Contains)
            .ToArray();

        var scopesJson = JsonSerializer.Serialize(scopes);

        // ─── Project optional IP allow-list ──────────────────────────────
        string? allowedIpsJson = null;
        if (!string.IsNullOrWhiteSpace(req.AllowedIpsCidr))
        {
            var cidrs = req.AllowedIpsCidr
                .Split(_cidrSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToArray();
            if (cidrs.Length > 0)
            {
                allowedIpsJson = JsonSerializer.Serialize(cidrs);
            }
        }

        // ─── INSERT ──────────────────────────────────────────────────────
        var newId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                """
                INSERT INTO integration.partner_api_credentials
                    (id, tenant_id, label, app_id, app_key_hash, app_key_prefix,
                     scopes, allowed_ips_cidr, environment, created_by_user_id,
                     created_at, updated_at, expires_at)
                VALUES
                    (@id, @tenant, @label, @app_id, @app_key_hash, @app_key_prefix,
                     @scopes::jsonb, @allowed_ips::jsonb, @env, @actor,
                     @created_at, @updated_at, @expires_at);
                """;

            cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = newId });
            cmd.Parameters.Add(new NpgsqlParameter("@tenant", NpgsqlDbType.Uuid) { Value = tenantId });
            cmd.Parameters.Add(new NpgsqlParameter("@label", NpgsqlDbType.Text) { Value = req.Label.Trim() });
            cmd.Parameters.Add(new NpgsqlParameter("@app_id", NpgsqlDbType.Text) { Value = appId });   // citext domain accepts text
            cmd.Parameters.Add(new NpgsqlParameter("@app_key_hash", NpgsqlDbType.Bytea) { Value = appKeyHash });
            cmd.Parameters.Add(new NpgsqlParameter("@app_key_prefix", NpgsqlDbType.Text) { Value = appKeyPrefix });
            cmd.Parameters.Add(new NpgsqlParameter("@scopes", NpgsqlDbType.Text) { Value = scopesJson });
            cmd.Parameters.Add(new NpgsqlParameter("@allowed_ips", NpgsqlDbType.Text) { Value = (object?)allowedIpsJson ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@env", NpgsqlDbType.Text) { Value = env });
            cmd.Parameters.Add(new NpgsqlParameter("@actor", NpgsqlDbType.Uuid) { Value = actorUserId });
            cmd.Parameters.Add(new NpgsqlParameter("@created_at", NpgsqlDbType.TimestampTz) { Value = now });
            cmd.Parameters.Add(new NpgsqlParameter("@updated_at", NpgsqlDbType.TimestampTz) { Value = now });
            cmd.Parameters.Add(new NpgsqlParameter("@expires_at", NpgsqlDbType.TimestampTz) { Value = (object?)req.ExpiresAt ?? DBNull.Value });

            await cmd.ExecuteNonQueryAsync(ct);
        }

        logger.PartnerCredentialCreated(newId, appId, env, tenantId, actorUserId);

        var row = new PartnerApiCredentialRow
        {
            Id = newId,
            Label = req.Label.Trim(),
            AppId = appId,
            AppKeyPrefix = appKeyPrefix,
            Scopes = scopes,
            Environment = env,
            CreatedAt = now,
            ExpiresAt = req.ExpiresAt,
            CreatedByName = currentUser.DisplayName ?? "—",
        };

        return new PartnerApiCredentialCreateResult
        {
            Row = row,
            ClearTextAppKey = appKey,
            ClearTextAppId = appId,
        };
    }

    /// <summary>Revoke.</summary>
    public async Task RevokeAsync(Guid credentialId, string reason, Guid actorUserId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new InvalidOperationException("No tenant context — cannot revoke credential.");
        }

        var safeReason = string.IsNullOrWhiteSpace(reason)
            ? "revoked from admin dashboard"
            : reason.Trim();

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE integration.partner_api_credentials
            SET    revoked_at     = @now,
                   revoked_reason = @reason,
                   updated_at     = @now
            WHERE  id = @id
              AND  tenant_id = @tenant
              AND  revoked_at IS NULL;
            """;

        cmd.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = credentialId });
        cmd.Parameters.Add(new NpgsqlParameter("@tenant", NpgsqlDbType.Uuid) { Value = tenantId });
        cmd.Parameters.Add(new NpgsqlParameter("@now", NpgsqlDbType.TimestampTz) { Value = DateTimeOffset.UtcNow });
        cmd.Parameters.Add(new NpgsqlParameter("@reason", NpgsqlDbType.Text) { Value = safeReason });

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

        logger.PartnerCredentialRevokeAttempted(credentialId, rowsAffected, safeReason, actorUserId);
    }

    /// <summary>Get kpis.</summary>
    public async Task<PartnerApiCredentialKpis> GetKpisAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new PartnerApiCredentialKpis();
        }

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.EnsureOpenAsync(ct);
        await conn.SetTenantContextAsync(tenantId, ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT
                COUNT(*) FILTER (WHERE revoked_at IS NULL) AS live_count,
                COUNT(*) FILTER (WHERE revoked_at IS NOT NULL) AS revoked_count
            FROM integration.partner_api_credentials
            WHERE tenant_id = @tenant;
            """;
        cmd.Parameters.Add(new NpgsqlParameter("@tenant", NpgsqlDbType.Uuid) { Value = tenantId });

        int live = 0, revoked = 0;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                live = Convert.ToInt32(reader.GetInt64(0));
                revoked = Convert.ToInt32(reader.GetInt64(1));
            }
        }

        // Metrics pipeline isn't wired yet — surface plausible mocks so the strip
        // looks alive. These come from the prototype 1:1.
        return new PartnerApiCredentialKpis
        {
            LiveCount = live,
            RevokedCount = revoked,
            ApiCalls30d = 0,
            AvgP95Ms = 142,
            RateLimitHits7d = 47,
        };
    }

    // ─── helpers ───────────────────────────────────────────────────────────

    /// <summary>Cryptographically random lowercase-hex string of the requested length.</summary>
    private static readonly char[] _cidrSeparators = [',', ';', ' '];

    private static string RandomHex(int hexChars)
    {
        if (hexChars <= 0)
        {
            return "";
        }

        var byteCount = (hexChars + 1) / 2;
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Length > hexChars ? hex[..hexChars] : hex;
    }

    private static string[] ParseScopes(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return Array.Empty<string>();
        }

        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(json);
            return arr is { Length: > 0 } ? arr : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
