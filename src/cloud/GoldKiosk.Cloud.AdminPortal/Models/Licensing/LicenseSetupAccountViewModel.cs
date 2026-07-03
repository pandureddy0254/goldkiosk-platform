using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Licensing;

/// <summary>License setup account view model.</summary>
public sealed class LicenseSetupAccountViewModel
{
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
