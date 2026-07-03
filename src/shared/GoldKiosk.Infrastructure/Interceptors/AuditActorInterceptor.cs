using System.Data;
using GoldKiosk.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GoldKiosk.Infrastructure.Interceptors;

/// <summary>
/// Runs immediately before <c>SaveChanges</c> to:
/// <list type="number">
///   <item>Issue <c>SELECT tenancy.set_actor_context(@user, @label)</c> so that the
///   PG-side audit triggers stamp the right actor.</item>
///   <item>Maintain <c>CreatedAt/UpdatedAt/CreatedByUserId/UpdatedByUserId</c> on any
///   entity that implements <see cref="IAuditableEntity"/>.</item>
/// </list>
/// </summary>
public sealed class AuditActorInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes a new instance of the <see cref="AuditActorInterceptor"/> class.</summary>
    public AuditActorInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await ApplyAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        }

        return result;
    }

    private async Task ApplyAsync(DbContext ctx, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId;
        var label = _currentUser.DisplayName;

        // Stamp audit columns on tracked entities.
        foreach (var entry in ctx.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.CreatedByUserId = userId;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
            }
        }

        // Set the PG actor GUC so the audit triggers see the right user.
        if (userId is not null)
        {
            var conn = ctx.Database.GetDbConnection();
            var opened = conn.State == ConnectionState.Closed;
            if (opened)
            {
                await conn.OpenAsync(ct).ConfigureAwait(false);
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT tenancy.set_actor_context($1, $2)";
                cmd.CommandType = CommandType.Text;

                var p1 = cmd.CreateParameter();
                p1.Value = userId.Value;
                cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter();
                p2.Value = label ?? string.Empty;
                cmd.Parameters.Add(p2);

                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                if (opened)
                {
                    await conn.CloseAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
