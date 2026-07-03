using System.Data;
using System.Data.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services.Common;

/// <summary>
/// Shared helpers for services that drop down to raw <see cref="DbConnection"/>
/// off the EF Core <c>DbContext</c> (i.e. <c>db.Database.GetDbConnection()</c>).
/// <para>
/// These exist because the manual-open + manual-tenant-context pattern leaked
/// into half a dozen services and each site re-implemented the same boilerplate.
/// The two invariants every <c>RawConn</c> call site must honour are:
/// </para>
/// <list type="number">
///   <item>The connection MUST be open before any command executes. If we opened
///   it ourselves (i.e. it was Closed when we found it), the caller is also
///   responsible for closing it — though in the request-pipeline case EF owns
///   the lifetime and closing prematurely will break the next EF call.</item>
///   <item>The PostgreSQL <c>app.tenant_id</c> GUC MUST be set on the connection
///   before any RLS-scoped query runs. The <c>TenantContextInterceptor</c>
///   sets this automatically on <c>ConnectionOpenedAsync</c> — but only when EF
///   owns the open. Direct <c>conn.OpenAsync()</c> bypasses the interceptor,
///   so RLS-scoped reads/writes return zero rows or error. Use
///   <see cref="SetTenantContextAsync"/> explicitly when in doubt.</item>
/// </list>
/// </summary>
internal static class DbConnectionExtensions
{
    /// <summary>
    /// Opens the connection if it isn't already open. Returns <c>true</c> when
    /// this call opened it (caller may wish to close it after use); <c>false</c>
    /// when it was already open (caller MUST NOT close it — EF owns it).
    /// </summary>
    public static async Task<bool> EnsureOpenAsync(this DbConnection conn, CancellationToken ct = default)
    {
        if (conn.State == ConnectionState.Open)
        {
            return false;
        }

        await conn.OpenAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Sets <c>app.tenant_id</c> on the current connection via the canonical
    /// <c>tenancy.set_tenant_context($1)</c> SECURITY DEFINER function.
    /// <para>
    /// Idempotent within a connection's lifetime — subsequent calls override
    /// the previous tenant. Throws if the connection is not open.
    /// </para>
    /// </summary>
    public static async Task SetTenantContextAsync(this DbConnection conn, Guid tenantId, CancellationToken ct = default)
    {
        if (conn.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection must be open before setting tenant context.");
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tenancy.set_tenant_context($1)";
        cmd.CommandType = CommandType.Text;
        var p = cmd.CreateParameter();
        p.Value = tenantId;
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
