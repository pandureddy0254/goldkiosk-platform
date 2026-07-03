using System.Net.Http;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Service selection: Sell or Pawn as two large glass cards plus inline terms acceptance
/// with a sheet. Nothing is posted per selection — everything accumulates into the
/// <see cref="SetupDto"/> and ships with the clubbed tray-open payload on Continue.
/// </summary>
public partial class WelcomeScreen : IDisposable
{
    private string? _serviceType;
    private bool _termsAccepted;
    private bool _showTerms;
    private bool _busy;
    private DateTimeOffset _enteredAt;

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

    private bool CanContinue => _serviceType is not null && _termsAccepted && !_busy;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _enteredAt = Time.GetUtcNow();
        Store.Changed += OnStoreChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);

    private void SelectService(string serviceType)
    {
        _serviceType = serviceType;
        Store.SelectService(serviceType);
    }

    private void ToggleTerms() => _termsAccepted = !_termsAccepted;

    private void OpenTerms()
    {
        _showTerms = true;
        Store.QueueClientEvent("terms_viewed");
    }

    private void CloseTerms() => _showTerms = false;

    private async Task ContinueAsync()
    {
        if (!CanContinue || Store.SessionId is not { } sessionId || _serviceType is not { } serviceType)
        {
            return;
        }

        _busy = true;
        try
        {
            double dwellMs = (Time.GetUtcNow() - _enteredAt).TotalMilliseconds;
            Store.QueueClientEvent("service_selected", new { ServiceType = serviceType, DwellMs = (long)dwellMs });

            var setup = new SetupDto(
                serviceType,
                Store.Locale,
                new TermsAcceptanceDto(Store.TermsVersion, Accepted: true, Time.GetLocalNow()),
                ItemHint: null,
                PromoCode: null);
            TrayStateResponse response = await Api.OpenTrayAsync(
                sessionId,
                new TrayOpenRequest("tray_open", setup, Store.DrainClientEvents()));
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

    private async Task CancelAsync() => await Flow.AbortAsync("user_cancel");
}
