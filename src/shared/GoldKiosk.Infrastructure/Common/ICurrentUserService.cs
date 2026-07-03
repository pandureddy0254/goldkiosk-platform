namespace GoldKiosk.Infrastructure.Common;

/// <summary>
/// Per-request information about the signed-in user.
/// Used by interceptors and services to scope queries by tenant and
/// stamp audit columns.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The signed-in user's id, or <c>null</c> if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The tenant the signed-in user belongs to, or <c>null</c> if anonymous.</summary>
    Guid? TenantId { get; }

    /// <summary>Cached display label used for audit rows.</summary>
    string? DisplayName { get; }

    /// <summary>True if a user is currently signed in.</summary>
    bool IsAuthenticated { get; }
}
