namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// Per-request information about the signed-in CRM staff user, read from the
/// cookie principal. Used by the connection interceptor (to set app.user_id),
/// the authorization filters, and the controllers (for RLS-equivalent query
/// scoping).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The signed-in profile id (crm.profiles.id), or <c>null</c> if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The signed-in user's role string (sales_manager | sales_rep | superadmin), or <c>null</c>.</summary>
    string? Role { get; }

    /// <summary>The signed-in user's full name, or <c>null</c>.</summary>
    string? FullName { get; }

    /// <summary>The signed-in user's email, or <c>null</c>.</summary>
    string? Email { get; }

    /// <summary>True if a user is currently signed in.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True if the user is a sales_manager or superadmin (manager-level access).</summary>
    bool IsManager { get; }

    /// <summary>True if the user is the superadmin.</summary>
    bool IsSuperadmin { get; }
}
