using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Auth;

/// <summary>
/// Posted by the "Activate with key" tab on /Account/Login. Carries the
/// activation key + the admin's profile + a new password. Server validates
/// against identity.activation_keys, creates the AppUser, marks the key
/// consumed, and signs the new admin in.
/// </summary>
public sealed class ActivateViewModel
{
    /// <summary>Gets or sets the activation key.</summary>
    [Required(ErrorMessage = "Activation key is required.")]
    [RegularExpression(@"^AIKI-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}$",
        ErrorMessage = "Format must be AIKI-XXXX-XXXX-XXXX (hexadecimal).")]
    [Display(Name = "Activation key")]
    public string ActivationKey { get; set; } = "";

    /// <summary>Gets or sets the email.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [Display(Name = "Admin email")]
    public string Email { get; set; } = "";

    /// <summary>Gets or sets the full name.</summary>
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    /// <summary>Gets or sets the password.</summary>
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 12,
        ErrorMessage = "At least 12 characters, with 1 number and 1 symbol.")]
    [Display(Name = "Choose a password")]
    public string Password { get; set; } = "";

    /// <summary>Gets or sets a value indicating whether accepts terms.</summary>
    [Required(ErrorMessage = "You must accept the Master Service Agreement.")]
    [Display(Name = "I accept the Master Service Agreement")]
    public bool AcceptsTerms { get; set; }
}
