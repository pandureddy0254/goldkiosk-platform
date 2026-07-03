using System.Data;
using System.Data.Common;
using GoldKiosk.Infrastructure.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GoldKiosk.Infrastructure.Interceptors;

/// <summary>
/// On every connection open, runs <c>SELECT tenancy.set_tenant_context(@p)</c>
/// against PostgreSQL so RLS policies evaluate against the correct tenant.
/// <para>
/// Reads the current tenant id from <see cref="ICurrentUserService"/>.
/// If no user is signed in (anonymous request, e.g. /Account/Login) the
/// interceptor skips silently — RLS will then return no rows.
/// </para>
/// </summary>
public sealed class TenantContextInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes a new instance of the <see cref="TenantContextInterceptor"/> class.</summary>
    public TenantContextInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT tenancy.set_tenant_context($1)";
        cmd.CommandType = CommandType.Text;
        var p = cmd.CreateParameter();
        p.Value = tenantId;
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        if (_currentUser.TenantId is not Guid tenantId)
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT tenancy.set_tenant_context($1)";
        cmd.CommandType = CommandType.Text;
        var p = cmd.CreateParameter();
        p.Value = tenantId;
        cmd.Parameters.Add(p);
        cmd.ExecuteNonQuery();
    }
}
