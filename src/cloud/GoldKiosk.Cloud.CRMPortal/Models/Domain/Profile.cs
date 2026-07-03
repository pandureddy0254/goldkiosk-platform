using System.ComponentModel.DataAnnotations.Schema;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Maps to <c>crm.profiles</c> — a CRM staff user (sales rep, manager, or superadmin).</summary>
public class Profile
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The user's full display name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Sign-in email (citext column).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Role string — one of <see cref="CrmRole"/>.</summary>
    public string Role { get; set; } = CrmRole.SalesRep;

    /// <summary>Optional avatar image URL.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>True when the user has manager-level access (sales_manager or superadmin).</summary>
    [NotMapped]
    public bool IsManager => Role is CrmRole.SalesManager or CrmRole.Superadmin;

    /// <summary>Up-to-two-letter initials derived from <see cref="FullName"/> for avatar chips.</summary>
    [NotMapped]
    public string Initials => string.IsNullOrWhiteSpace(FullName)
        ? "??"
        : string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0])).ToUpperInvariant();
}
