using System.Data;
using System.Data.Common;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GoldKiosk.Cloud.CRMPortal.Data;

/// <summary>
/// On every connection open, runs <c>SELECT set_config('app.user_id', $1, false)</c>
/// so the SECURITY DEFINER RPCs and the audit triggers see the signed-in profile
/// id via <c>crm.current_user_id()</c>, and RLS policies scope correctly.
/// <para>
/// The user id is read from <see cref="ICurrentUserService"/>. When no user is
/// signed in (anonymous request — e.g. /Account/Login, the public JWKS endpoint)
/// the GUC is set to the empty string, which <c>crm.current_user_id()</c> maps to
/// NULL — RLS then returns no rows for the app role.
/// </para>
/// <para>
/// The GUC is set with session scope (third arg <c>false</c>) so it survives the
/// implicit-transaction boundaries EF uses for individual reads. It is re-applied
/// on every physical connection open, so a pooled connection always carries the
/// current request's identity.
/// </para>
/// </summary>
public sealed class UserContextInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the interceptor with the per-request user accessor.</summary>
    /// <param name="currentUser">Source of the signed-in profile id.</param>
    public UserContextInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var cmd = CreateSetUserCommand(connection);
        await using (cmd.ConfigureAwait(false))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        using var cmd = CreateSetUserCommand(connection);
        cmd.ExecuteNonQuery();
    }

    private DbCommand CreateSetUserCommand(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        // false = session-scoped (not LOCAL) so it persists across EF's per-query
        // implicit transactions. Re-applied on every open.
        cmd.CommandText = "SELECT set_config('app.user_id', $1, false)";
        cmd.CommandType = CommandType.Text;
        var p = cmd.CreateParameter();
        p.Value = _currentUser.UserId?.ToString() ?? string.Empty;
        cmd.Parameters.Add(p);
        return cmd;
    }
}
