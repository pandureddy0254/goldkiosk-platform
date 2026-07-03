using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Overlays;

/// <summary>
/// The live-agent escalation panel: shows while an agent review is connecting or in
/// progress (<c>agent_status</c> events), with a glass video placeholder. Approved,
/// declined and unavailable states hide the panel — the session flow carries the outcome.
/// </summary>
public partial class AgentOverlay : IDisposable
{
    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private bool IsVisible => Store.AgentStatus?.Status is "connecting" or "agent_joined";

    private string Title => Store.AgentStatus?.Status == "agent_joined"
        ? L["agent.joined.title"]
        : L["agent.connecting.title"];

    private string Subtitle => Store.AgentStatus?.Status == "agent_joined"
        ? L["agent.joined.subtitle"]
        : L["agent.connecting.subtitle"];

    /// <inheritdoc />
    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);
}
