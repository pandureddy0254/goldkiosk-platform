using System.Security.Cryptography;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services.Common;

/// <summary>
/// Idempotent dev/test seeder. On Development startup, ensures:
///   * Tenant <c>GK-DEV</c> exists (the dev/test sandbox).
///   * Tenant <c>EGB-AE-001</c> "Emirates Gold Bank PJSC" exists (the design's
///     reference customer-flow tenant).
///   * Admin user <c>admin@goldkiosk.local</c> / ChangeMe!123 exists on GK-DEV.
///   * The system RBAC roles are bootstrapped for both tenants.
///   * A live activation key <c>AIKI-7F2A-9C31-B4E8</c> exists against the
///     Emirates Gold Bank tenant for the design demo flow.
/// </summary>
public static class DevStartupSeeder
{
    /// <summary>New.</summary>
    public static readonly Guid DevTenantId = new("00000000-0000-0000-0000-000000000001");
    /// <summary>New.</summary>
    public static readonly Guid EgbTenantId = new("a4f8e2c1-9b3d-4e87-a5c2-1f8e3b9d4a6c");

    /// <summary>Admin email.</summary>
    public const string AdminEmail = "admin@goldkiosk.local";
    /// <summary>Admin password.</summary>
    public const string AdminPassword = "ChangeMe!123";

    /// <summary>Cleartext activation key shown in the design mock-ups.</summary>
    public const string DemoActivationKey = "AIKI-7F2A-9C31-B4E8";
    /// <summary>Demo activation email.</summary>
    public const string DemoActivationEmail = "cio@emiratesgold.ae";

    /// <summary>Seed.</summary>
    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var conn = db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);

        try
        {
            // ── Pin to GK-DEV for the dev-tenant inserts ────────────────────
            await conn.SetTenantContextAsync(DevTenantId, ct);

            // GK-DEV tenant
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenants
                        (id, code, legal_name, tier, home_country_code, data_residency_region, status, onboarded_at)
                    VALUES
                        (@id, 'GK-DEV', 'GoldKiosk Development', 'standard', 'AE', 'uaenorth', 'active', now())
                    ON CONFLICT (code) DO NOTHING";
                AddParam(cmd, "id", DevTenantId);
                if (await cmd.ExecuteNonQueryAsync(ct) > 0)
                {
                    logger.DevTenantSeeded(DevTenantId);
                }
            }

            // GK-DEV admin user
            var existing = await userManager.FindByEmailAsync(AdminEmail);
            if (existing is null)
            {
                var user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = DevTenantId,
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FirstName = "Dev",
                    LastName = "Admin",
                    OtpMode = "off",
                    Status = "active",
                    Locale = "en-AE",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                var result = await userManager.CreateAsync(user, AdminPassword);
                if (result.Succeeded)
                {
                    // PII discipline: log the user id, never the email or password.
                    logger.DevAdminSeeded(user.Id);

                    // Assign Owner role on the dev tenant so the admin has full access.
                    await AssignOwnerRole(conn, user.Id, DevTenantId, ct);
                }
                else
                {
                    var errs = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.DevAdminSeedFailed(errs);
                }
            }
            else
            {
                logger.DevAdminExists(existing.Id);
                // Make sure they still hold Owner — idempotent.
                await AssignOwnerRole(conn, existing.Id, DevTenantId, ct);
            }

            // ── Seed Emirates Gold Bank tenant + activation key ─────────────
            await conn.SetTenantContextAsync(EgbTenantId, ct);

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenants
                        (id, code, legal_name, tier, home_country_code, data_residency_region, status, onboarded_at)
                    VALUES
                        (@id, 'EGB-AE-001', 'Emirates Gold Bank PJSC', 'bank', 'AE', 'uaenorth', 'active', now())
                    ON CONFLICT (code) DO NOTHING";
                AddParam(cmd, "id", EgbTenantId);
                if (await cmd.ExecuteNonQueryAsync(ct) > 0)
                {
                    logger.EgbTenantSeeded();
                }
            }

            // Apply system RBAC roles to BOTH tenants (idempotent).
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT identity.apply_system_roles(@dev_id);
                    SELECT identity.apply_system_roles(@egb_id);";
                AddParam(cmd, "dev_id", DevTenantId);
                AddParam(cmd, "egb_id", EgbTenantId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Insert the demo activation key (hash, never cleartext).
            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(DemoActivationKey.ToUpperInvariant()));
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO identity.activation_keys
                        (tenant_id, key_prefix, key_hash, issued_to_email, expires_at, issued_by_actor)
                    SELECT
                        @tid,
                        'AIKI-7F2A',
                        @hash,
                        @email,
                        now() + interval '30 days',
                        'dev-seeder'
                    WHERE NOT EXISTS (
                        SELECT 1 FROM identity.activation_keys
                         WHERE tenant_id = @tid
                           AND consumed_at IS NULL
                           AND revoked_at  IS NULL
                    )";
                AddParam(cmd, "tid", EgbTenantId);
                AddParam(cmd, "hash", keyHash);
                AddParam(cmd, "email", DemoActivationEmail);

                if (await cmd.ExecuteNonQueryAsync(ct) > 0)
                {
                    // PII discipline: log the key prefix only, never the full key or email.
                    logger.DemoActivationKeySeeded("AIKI-7F2A");
                }
            }
        }
        finally
        {
            // Only close the connection if we were the ones who opened it.
            // The seeder runs once at startup so this is rarely a concern,
            // but be a polite citizen — leaving a closed connection is
            // exactly the state EF expects to find on next acquisition.
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static async Task AssignOwnerRole(System.Data.Common.DbConnection conn, Guid userId, Guid tenantId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO identity.user_roles (user_id, role_id, granted_at)
            SELECT @uid, r.id, now()
              FROM identity.roles r
             WHERE r.tenant_id = @tid
               AND r.code      = 'owner'
               AND r.is_active = true
               AND NOT EXISTS (
                   SELECT 1
                     FROM identity.user_roles ur
                    WHERE ur.user_id = @uid
                      AND ur.role_id = r.id
                      AND ur.revoked_at IS NULL
               )";
        AddParam(cmd, "uid", userId);
        AddParam(cmd, "tid", tenantId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
