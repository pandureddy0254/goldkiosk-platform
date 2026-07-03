using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Invitations;

/// <summary>Gets the invite user view model.</summary>
/// <summary>Invite user view model.</summary>
public sealed class InviteUserViewModel
{
    /// <summary>Gets or sets the email.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Work email")]
    public string Email { get; set; } = "";

    /// <summary>Gets or sets the first name.</summary>
    [StringLength(80)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the last name.</summary>
    [StringLength(80)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    /// <summary>Gets or sets the role id.</summary>
    [Required(ErrorMessage = "Pick a role to assign on join.")]
    [Display(Name = "Role")]
    public Guid RoleId { get; set; }

    /// <summary>Gets or sets the available roles.</summary>
    [Display(Name = "Available roles")]
    public List<RoleOption> AvailableRoles { get; set; } = new();
}

/// <summary>Role option.</summary>
public sealed class RoleOption
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = "";
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = "";
}

/// <summary>Accept invite view model.</summary>
public sealed class AcceptInviteViewModel
{
    /// <summary>Gets or sets the token.</summary>
    [Required] public string Token { get; set; } = "";

    /// <summary>Gets or sets the first name.</summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(80)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    /// <summary>Gets or sets the last name.</summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(80)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    /// <summary>Gets or sets the password.</summary>
    [Required(ErrorMessage = "Choose a password.")]
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
