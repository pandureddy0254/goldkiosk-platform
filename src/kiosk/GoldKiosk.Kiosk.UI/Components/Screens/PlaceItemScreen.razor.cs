using System.Net.Http;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Place-item: tray opening/awaiting/closing visuals driven by <c>tray_state_changed</c>.
/// Confirm sends the clubbed tray-close with <c>has_item: true</c> (triggers analysis);
/// backing out closes without an item and returns to welcome.
/// </summary>
public partial class PlaceItemScreen : IDisposable
{
    private bool _busy;

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private string Title => Store.TrayStatus switch
    {
        "opening" => L["place.title.opening"],
        "open" => L["place.title.open"],
        "closing" => L["place.title.closing"],
        _ => L["place.title.closed"],
    };

    private string TrayClass => Store.TrayStatus switch
    {
        "opening" => "tray-opening",
        "open" => "tray-open",
        "closing" => "tray-closing",
        _ => "tray-closed",
    };

    private bool CanConfirm => Store.TrayStatus == "open" && !_busy;

    /// <inheritdoc />
    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task ConfirmAsync()
    {
        Store.QueueClientEvent("item_placed_confirmed", new { Retries = 0 });
        await CloseTrayAsync(hasItem: true);
    }

    private async Task BackOutAsync()
    {
        Store.QueueClientEvent("place_item_backed_out");
        await CloseTrayAsync(hasItem: false);
    }

    private async Task CloseTrayAsync(bool hasItem)
    {
        if (_busy || Store.SessionId is not { } sessionId)
        {
            return;
        }

        _busy = true;
        try
        {
            TrayStateResponse response = await Api.CloseTrayAsync(
                sessionId,
                new TrayCloseRequest("tray_close", hasItem, Store.DrainClientEvents()));
            Store.ApplyTray(response);
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            Flow.ShowApiError(ex);
        }
        finally
        {
            _busy = false;
        }
    }
}
