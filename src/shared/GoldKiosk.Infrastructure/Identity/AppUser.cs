using Microsoft.AspNetCore.Identity;

namespace GoldKiosk.Infrastructure.Identity;

/// <summary>
/// Application user, mapped onto the <c>identity.users</c> table.
/// Inherits from <see cref="IdentityUser{Guid}"/> so the ASP.NET Core Identity
/// pipeline (sign-in, password hashing, lockout, 2FA) works out of the box,
/// while the extra columns below map onto the additional fields in our schema.
/// <para>
/// The Identity <c>UserName</c> column is mapped onto our <c>email</c> column —
/// we don't use a separate username.
/// </para>
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Subject claim from Microsoft Entra ID when SSO is used. Null for local-only accounts.</summary>
    public string? ExternalSubject { get; set; }

    /// <summary><c>email</c> | <c>sms</c> | <c>totp</c> | <c>webauthn</c> | <c>off</c>.</summary>
    public string OtpMode { get; set; } = "off";

    /// <summary><c>invited</c> | <c>active</c> | <c>suspended</c> | <c>locked</c>.</summary>
    public string Status { get; set; } = "invited";

    /// <summary>Gets or sets the locale.</summary>
    public string Locale { get; set; } = "en";

    /// <summary>Gets or sets a value indicating whether password reset required.</summary>
    public bool PasswordResetRequired { get; set; }

    /// <summary>Gets or sets the last signed in at.</summary>
    public DateTimeOffset? LastSignedInAt { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the updated at.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
