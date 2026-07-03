namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>
/// Maps to <c>crm.user_preferences</c> (one row per <c>crm.profiles(id)</c>).
/// RLS: a user can only read/write their own row.
/// </summary>
public class UserPreference
{
    /// <summary>PK and FK to <c>crm.profiles(id)</c>. Supplied by the caller, not server-generated.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// UI colour scheme. Allowed values: "light" | "dark". Defaults to "light".
    /// The DB enforces the CHECK constraint; C# should treat any other value as "light".
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>Server-managed. Postgres sets it via trigger; never written on insert/update.</summary>
    public DateTime UpdatedAt { get; set; }
}
