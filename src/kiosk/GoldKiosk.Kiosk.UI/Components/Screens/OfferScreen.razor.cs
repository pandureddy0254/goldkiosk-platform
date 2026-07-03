using System.Globalization;
using System.Net.Http;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// The offer reveal (Obsidian offer export): mono header chips, one huge serif amount in
/// the platinum gradient, Verified/Live-price/lock-countdown chips, the AI explainer
/// sheet, and Accept / Return item. The countdown comes from the offer's
/// <c>expires_at</c>; an expired lock disables Accept.
/// </summary>
public partial class OfferScreen : IDisposable
{
    private ITimer? _tick;
    private string _countdown = "--:--";
    private bool _expired;
    private bool _busy;
    private bool _showExplain;
    private ExplainOfferResponse? _explanation;

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private TimeProvider Time { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Store.Changed += OnStoreChanged;
        UpdateCountdown();
        _tick = Time.CreateTimer(_ => OnTick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        if (Store.Offer is null)
        {
            // Crash/reload resume: the snapshot carries the offer.
            _ = Store.ResyncAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        _tick?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string AmountMain(OfferDto offer)
    {
        string display = offer.Amount.Display;
        int dot = display.LastIndexOf('.');
        return dot > 0 ? display[..dot] : display;
    }

    private static string AmountMinor(OfferDto offer)
    {
        string display = offer.Amount.Display;
        int dot = display.LastIndexOf('.');
        return dot > 0 ? display[dot..] : string.Empty;
    }

    private static string FormatApr(decimal aprPercent) =>
        string.Create(CultureInfo.InvariantCulture, $"{aprPercent:0.#}%");

    private string FormatDate(DateOnly date) =>
        date.ToString("d MMM yyyy", CultureInfo.GetCultureInfo(L.Locale));

    private void OnStoreChanged() => _ = InvokeAsync(() =>
    {
        UpdateCountdown();
        StateHasChanged();
    });

    private void OnTick()
    {
        UpdateCountdown();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateCountdown()
    {
        if (Store.Offer is not { } offer)
        {
            return;
        }

        TimeSpan remaining = offer.ExpiresAt - Time.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            _expired = true;
            _countdown = "00:00";
            return;
        }

        _expired = false;
        _countdown = string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}");
    }

    private async Task AcceptAsync()
    {
        if (_busy || Store.SessionId is not { } sessionId)
        {
            return;
        }

        _busy = true;
        try
        {
            SessionActionResponse response = await Api.AcceptOfferAsync(sessionId);
            Store.ApplyAction(response);
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

    private async Task DeclineAsync()
    {
        if (_busy || Store.SessionId is not { } sessionId)
        {
            return;
        }

        _busy = true;
        try
        {
            SessionActionResponse response = await Api.DeclineOfferAsync(sessionId);
            Store.ApplyAction(response);
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

    private async Task OpenExplainAsync()
    {
        _showExplain = true;
        if (_explanation is not null || Store.SessionId is not { } sessionId)
        {
            return;
        }

        try
        {
            _explanation = await Api.ExplainOfferAsync(sessionId, new ExplainOfferRequest(Question: null));
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            _showExplain = false;
            Flow.ShowApiError(ex);
        }
    }

    private void CloseExplain() => _showExplain = false;
}
