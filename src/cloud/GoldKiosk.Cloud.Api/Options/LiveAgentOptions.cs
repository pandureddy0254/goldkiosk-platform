using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// Live-agent escalation policy. Reviews that receive no agent verdict within the TTL
/// end as <c>timed_out</c> — the fail-closed rule; auto-approval does not exist.
/// </summary>
public sealed class LiveAgentOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "LiveAgent";

    /// <summary>Gets the review TTL in seconds before a pending review times out.</summary>
    [Range(30, 3600)]
    public int ReviewTtlSeconds { get; init; } = 300;
}
