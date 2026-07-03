using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// Loads the signed-in staff user's <see cref="Profile"/> row. Replaces the old
/// Supabase-backed profile lookup; now reads crm.profiles via EF Core, scoped by
/// the cookie principal's user id (<see cref="ICurrentUserService"/>).
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the signed-in user's profile, or <c>null</c> when anonymous/unknown.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Profile?> GetCurrentAsync(CancellationToken ct = default);
}

/// <summary>Default <see cref="IProfileService"/> with per-request memoisation.</summary>
public sealed class ProfileService : IProfileService
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private Profile? _cached;
    private bool _loaded;

    /// <summary>Initializes the service with the CRM database context and current-user accessor.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity.</param>
    public ProfileService(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public async Task<Profile?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_loaded)
        {
            return _cached;
        }

        _loaded = true;

        if (_currentUser.UserId is not Guid uid)
        {
            return _cached = null;
        }

        _cached = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == uid, ct)
            .ConfigureAwait(false);
        return _cached;
    }
}
