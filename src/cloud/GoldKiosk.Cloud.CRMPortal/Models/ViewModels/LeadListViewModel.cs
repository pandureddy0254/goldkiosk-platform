using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>View model shared by the leads table and kanban views.</summary>
public class LeadListViewModel
{
    /// <summary>The visible (RLS-scoped, filtered) leads.</summary>
    public IReadOnlyList<Lead> Leads { get; init; } = [];

    /// <summary>Profiles for every owner appearing in <see cref="Leads"/>.</summary>
    public IReadOnlyDictionary<Guid, Profile> Owners { get; init; } = new Dictionary<Guid, Profile>();

    /// <summary>Stage filter value echoed back to the form.</summary>
    public string? Stage { get; init; }

    /// <summary>Source filter value echoed back to the form.</summary>
    public string? Source { get; init; }

    /// <summary>Owner filter value echoed back to the form.</summary>
    public string? OwnerId { get; init; }

    /// <summary>Free-text search echoed back to the form.</summary>
    public string? Query { get; init; }

    /// <summary>Total number of visible leads.</summary>
    public int TotalCount => Leads.Count;

    /// <summary>Counts leads currently in <paramref name="stage"/>.</summary>
    /// <param name="stage">A <see cref="LeadStage"/> value.</param>
    public int CountInStage(string stage) =>
        Leads.Count(l => string.Equals(l.Stage, stage, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the leads currently in <paramref name="stage"/>.</summary>
    /// <param name="stage">A <see cref="LeadStage"/> value.</param>
    public IEnumerable<Lead> InStage(string stage) =>
        Leads.Where(l => string.Equals(l.Stage, stage, StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves an owner id to avatar initials (em-dash when unassigned).</summary>
    /// <param name="ownerId">The owner's profile id, or <c>null</c> when unassigned.</param>
    public string OwnerInitials(Guid? ownerId)
    {
        if (ownerId is null)
        {
            return "—";
        }

        return Owners.TryGetValue(ownerId.Value, out var p) ? p.Initials : "??";
    }

    /// <summary>Resolves an owner id to a display name ("Unassigned" when null).</summary>
    /// <param name="ownerId">The owner's profile id, or <c>null</c> when unassigned.</param>
    public string OwnerName(Guid? ownerId)
    {
        if (ownerId is null)
        {
            return "Unassigned";
        }

        return Owners.TryGetValue(ownerId.Value, out var p) ? p.FullName : "—";
    }
}
