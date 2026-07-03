using Newtonsoft.Json.Linq;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>
/// Mirrors <c>crm.lead_activities.activity_type</c> (text column). Meeting / SystemEvent
/// are kept as view-side fallback labels but never persist (DB check rejects them).
/// </summary>
public static class ActivityType
{
    /// <summary>Free-text note added by a rep.</summary>
    public const string Note = "note";

    /// <summary>Logged phone call.</summary>
    public const string Call = "call";

    /// <summary>Logged email touchpoint.</summary>
    public const string Email = "email";

    /// <summary>Pipeline stage change (written by the move_lead_stage RPC).</summary>
    public const string StageChange = "stage_change";

    /// <summary>Proposal sent to the prospect.</summary>
    public const string ProposalSent = "proposal_sent";

    /// <summary>Lead marked won.</summary>
    public const string Won = "won";

    /// <summary>Lead marked lost.</summary>
    public const string Lost = "lost";

    /// <summary>View fallback label only — never persisted.</summary>
    public const string Meeting = "meeting";

    /// <summary>View fallback label only — never persisted.</summary>
    public const string SystemEvent = "system_event";
}

/// <summary>Maps to <c>crm.lead_activities</c> — the per-lead activity timeline.</summary>
public class LeadActivity
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The lead this activity belongs to.</summary>
    public Guid LeadId { get; set; }

    /// <summary>The staff profile that performed the activity, or <c>null</c> for system events.</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Activity kind — one of <see cref="ActivityType"/>. Column name is <c>activity_type</c>.</summary>
    public string Type { get; set; } = ActivityType.Note;

    /// <summary>
    /// jsonb in Postgres — <see cref="JObject"/> lets the view pull fields out
    /// (<c>a.Payload?["summary"]?.ToString()</c>) without tripping the string deserializer.
    /// </summary>
    public JObject? Payload { get; set; }

    /// <summary>When the activity occurred (DB default; never written by the app).</summary>
    public DateTime OccurredAt { get; set; }
}
