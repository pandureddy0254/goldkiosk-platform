using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>Form model for the sign-in page.</summary>
public class LoginViewModel
{
    /// <summary>Sign-in email address.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Sign-in password.</summary>
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Local URL to return to after sign-in (validated server-side).</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Whether to keep the session cookie sliding.</summary>
    public bool KeepSignedIn { get; set; } = true;
}
