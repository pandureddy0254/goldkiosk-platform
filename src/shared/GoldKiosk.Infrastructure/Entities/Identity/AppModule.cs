namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// Registry of every dashboard module/action that can have a permission attached.
/// Used by the AccessControl page to render the permissions matrix.
/// </summary>
public sealed class AppModule
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the controller.</summary>
    public string Controller { get; set; } = string.Empty;
    /// <summary>Gets or sets the action.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the module name.</summary>
    public string ModuleName { get; set; } = string.Empty;
    /// <summary>Gets or sets the sub module.</summary>
    public string? SubModule { get; set; }
    /// <summary>Gets or sets the sub sub module.</summary>
    public string? SubSubModule { get; set; }

    /// <summary><c>page</c> | <c>api</c> | <c>job</c>.</summary>
    public string Type { get; set; } = "page";

    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
