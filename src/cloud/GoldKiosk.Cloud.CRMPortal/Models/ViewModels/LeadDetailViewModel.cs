using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>View model for the lead detail page and drawer partial.</summary>
public class LeadDetailViewModel
{
    /// <summary>The lead being displayed.</summary>
    public Lead Lead { get; init; } = new();

    /// <summary>The lead's activity timeline, oldest first.</summary>
    public IReadOnlyList<LeadActivity> Activities { get; init; } = [];

    /// <summary>Profiles for every actor appearing in <see cref="Activities"/>.</summary>
    public IReadOnlyDictionary<Guid, Profile> Actors { get; init; } = new Dictionary<Guid, Profile>();

    /// <summary>The owning rep's profile, when resolved.</summary>
    public Profile? Owner { get; init; }

    /// <summary>Resolves an actor id to a display name ("system" when null, em-dash when unknown).</summary>
    /// <param name="actorId">The actor's profile id, or <c>null</c> for system events.</param>
    public string ActorName(Guid? actorId)
    {
        if (actorId is null)
        {
            return "system";
        }

        return Actors.TryGetValue(actorId.Value, out var p) ? p.FullName : "—";
    }
}
