using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>Form model for the change-password card on the Settings page.</summary>
public class ChangePasswordViewModel
{
    /// <summary>The user's current password, re-verified before rotation.</summary>
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password (min 8 characters; the DB RPC enforces the same).</summary>
    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation that must match <see cref="NewPassword"/>.</summary>
    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The confirmation does not match the new password.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
