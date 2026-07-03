using System.Security.Cryptography;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services.Invitations;

/// <summary>
/// Reads/writes identity.user_invitations via raw SQL on the AppDbContext
/// connection so the same tenant-context interceptor + RLS rules apply.
/// </summary>
public sealed class UserInvitationService : IUserInvitationService
{
    private const int TokenBytes = 32;   // 256-bit URL-safe token
    private const int LifetimeDays = 7;

    private readonly AppDbContext _db;
    private readonly ILogger<UserInvitationService> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserInvitationService"/> class.</summary>
    public UserInvitationService(AppDbContext db, ILogger<UserInvitationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Create.</summary>
    public async Task<CreateInviteResult> CreateAsync(
        Guid tenantId, Guid invitedByUserId,
        string inviteeEmail, string firstName, string lastName,
        Guid roleId, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var hash = HashToken(token);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(LifetimeDays);

        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            await conn.SetTenantContextAsync(tenantId, ct);

            // Revoke any existing live invite for the same email so we never have
            // two open tokens for one address (UQ index ux_user_invitations__live_per_email
            // would error otherwise).
            await using (var revoke = conn.CreateCommand())
            {
                revoke.CommandText = @"
                    UPDATE identity.user_invitations
                       SET revoked_at = now(),
                           revoked_reason = 'superseded by new invite',
                           updated_at = now()
                     WHERE tenant_id = @tid
                       AND lower(invitee_email::text) = lower(@email)
                       AND accepted_at IS NULL
                       AND revoked_at  IS NULL";
                Add(revoke, "tid", tenantId);
                Add(revoke, "email", inviteeEmail.Trim());
                await revoke.ExecuteNonQueryAsync(ct);
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO identity.user_invitations
                    (tenant_id, invitee_email, invitee_first_name, invitee_last_name,
                     role_id, token_hash, invited_by_user_id, expires_at)
                VALUES
                    (@tid, @email, @first, @last, @rid, @hash, @uid, @expires)
                RETURNING id";
            Add(cmd, "tid", tenantId);
            Add(cmd, "email", inviteeEmail.Trim());
            Add(cmd, "first", string.IsNullOrWhiteSpace(firstName) ? (object)DBNull.Value : firstName.Trim());
            Add(cmd, "last", string.IsNullOrWhiteSpace(lastName) ? (object)DBNull.Value : lastName.Trim());
            Add(cmd, "rid", roleId);
            Add(cmd, "hash", hash);
            Add(cmd, "uid", invitedByUserId);
            Add(cmd, "expires", expiresAt);

            var id = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
            _logger.InvitationCreated(id, tenantId, roleId, expiresAt);
            return new CreateInviteResult(id, token, expiresAt);
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>List pending.</summary>
    public async Task<List<PendingInviteRow>> ListPendingAsync(Guid tenantId, CancellationToken ct = default)
    {
        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            await conn.SetTenantContextAsync(tenantId, ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT i.id, i.invitee_email, coalesce(i.invitee_first_name,'') || ' ' || coalesce(i.invitee_last_name,'') AS full_name,
                       r.name, i.invited_at, i.expires_at, i.resent_count
                  FROM identity.user_invitations i
                  JOIN identity.roles r ON r.id = i.role_id
                 WHERE i.tenant_id = @tid
                   AND i.accepted_at IS NULL
                   AND i.revoked_at  IS NULL
                 ORDER BY i.invited_at DESC";
            Add(cmd, "tid", tenantId);

            var rows = new List<PendingInviteRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new PendingInviteRow
                {
                    Id = reader.GetGuid(0),
                    InviteeEmail = reader.GetString(1),
                    FullName = reader.GetString(2).Trim(),
                    RoleName = reader.GetString(3),
                    InvitedAt = reader.GetFieldValue<DateTimeOffset>(4),
                    ExpiresAt = reader.GetFieldValue<DateTimeOffset>(5),
                    ResentCount = reader.GetInt32(6),
                });
            }
            return rows;
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>Resolve.</summary>
    public async Task<InvitationDetails?> ResolveAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = HashToken(token);

        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            // Token is anonymous (not yet authenticated) → use the dev tenant as
            // a no-op context. The query selects by hash, which is globally unique.
            await conn.SetTenantContextAsync(DevStartupSeeder.DevTenantId, ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT i.id, i.tenant_id, i.invitee_email,
                       coalesce(i.invitee_first_name,''), coalesce(i.invitee_last_name,''),
                       i.role_id, r.code, r.name,
                       t.legal_name, i.expires_at
                  FROM identity.user_invitations i
                  JOIN identity.roles    r ON r.id = i.role_id
                  JOIN tenancy.tenants   t ON t.id = i.tenant_id
                 WHERE i.token_hash  = @hash
                   AND i.accepted_at IS NULL
                   AND i.revoked_at  IS NULL
                   AND i.expires_at  > now()";
            Add(cmd, "hash", hash);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            return new InvitationDetails
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1),
                InviteeEmail = reader.GetString(2),
                FirstName = reader.GetString(3),
                LastName = reader.GetString(4),
                RoleId = reader.GetGuid(5),
                RoleCode = reader.GetString(6),
                RoleName = reader.GetString(7),
                TenantLegalName = reader.GetString(8),
                ExpiresAt = reader.GetFieldValue<DateTimeOffset>(9),
            };
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>Mark accepted.</summary>
    public async Task MarkAcceptedAsync(Guid invitationId, Guid acceptedUserId, CancellationToken ct = default)
    {
        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            // We don't know the tenant_id of this invitation here, but RLS will
            // still permit the UPDATE because the row has tenant_id set and we
            // restrict by id. Use the dev tenant as a non-empty placeholder.
            await conn.SetTenantContextAsync(DevStartupSeeder.DevTenantId, ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE identity.user_invitations
                   SET accepted_at      = now(),
                       accepted_user_id = @uid,
                       updated_at       = now()
                 WHERE id = @id
                   AND accepted_at IS NULL
                   AND revoked_at  IS NULL";
            Add(cmd, "uid", acceptedUserId);
            Add(cmd, "id", invitationId);

            if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            {
                throw new InvalidOperationException(
                    "Invitation has already been accepted or revoked.");
            }
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>Revoke.</summary>
    public async Task RevokeAsync(Guid invitationId, string reason, CancellationToken ct = default)
    {
        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE identity.user_invitations
                   SET revoked_at     = now(),
                       revoked_reason = @reason,
                       updated_at     = now()
                 WHERE id = @id
                   AND accepted_at IS NULL
                   AND revoked_at  IS NULL";
            Add(cmd, "reason", reason);
            Add(cmd, "id", invitationId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>Regenerate token.</summary>
    public async Task<string> RegenerateTokenAsync(Guid invitationId, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var hash = HashToken(token);

        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE identity.user_invitations
                   SET token_hash     = @hash,
                       resent_count   = resent_count + 1,
                       last_resent_at = now(),
                       expires_at     = greatest(expires_at, now() + interval '7 days'),
                       updated_at     = now()
                 WHERE id = @id
                   AND accepted_at IS NULL
                   AND revoked_at  IS NULL";
            Add(cmd, "hash", hash);
            Add(cmd, "id", invitationId);
            if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            {
                throw new InvalidOperationException("Invitation cannot be resent — already accepted, revoked, or missing.");
            }

            return token;
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static string GenerateToken()
    {
        Span<byte> buf = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToBase64String(buf)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] HashToken(string token) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(token));

    private static void Add(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
