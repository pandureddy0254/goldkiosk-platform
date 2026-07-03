namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// Assignment of a role to a user. Optionally scoped to a single store
/// (e.g. a Branch Manager is only a manager at one store, not the tenant overall).
/// </summary>
public sealed class AppUserRole
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the user id.</summary>
    public Guid UserId { get; set; }
    /// <summary>Gets or sets the role id.</summary>
    public Guid RoleId { get; set; }

    /// <summary>Optional FK to <c>store.stores</c> when the role is store-scoped.</summary>
    public Guid? StoreId { get; set; }

    /// <summary>Gets or sets the granted at.</summary>
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the granted by user id.</summary>
    public Guid? GrantedByUserId { get; set; }

    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets the role.</summary>
    public AppRole? Role { get; set; }
}
