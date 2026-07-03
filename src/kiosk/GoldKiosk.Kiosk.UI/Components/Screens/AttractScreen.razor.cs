using System.Globalization;
using System.Net.Http;
using GoldKiosk.Contracts.V1.Rates;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// The attract loop (Obsidian intro): rotating serif hero words — We buy Gold/Silver ·
/// Pawn Gold/Silver — the tray medallion with its progress ring, a display-safe rates
/// ticker, and the pulsing TOUCH TO BEGIN control that begins a session.
/// </summary>
public partial class AttractScreen : IDisposable
{
    private static readonly (string PrefixKey, string WordKey)[] _loop =
    [
        ("attract.we_buy", "attract.word.gold"),
        ("attract.we_buy", "attract.word.silver"),
        ("attract.pawn", "attract.word.gold"),
        ("attract.pawn", "attract.word.silver"),
    ];

    private RatesResponse? _rates;
    private int _wordIndex;
    private bool _busy;
    private ITimer? _cycleTimer;

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private KioskUiOptions Options { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private TimeProvider Time { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private string CurrentPrefix => L[_loop[_wordIndex].PrefixKey];

    private string CurrentWord => L[_loop[_wordIndex].WordKey];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        TimeSpan cycle = TimeSpan.FromMilliseconds(Options.AttractWordCycleMs);
        _cycleTimer = Time.CreateTimer(_ => AdvanceWord(), null, cycle, cycle);

        try
        {
            _rates = await Api.GetRatesAsync();
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            // The ticker is decorative; the attract loop works without it.
            _rates = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cycleTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string FormatChange(decimal changePercent) =>
        string.Create(CultureInfo.InvariantCulture, $"{(changePercent >= 0 ? "+" : "")}{changePercent:0.00}%");

    private void AdvanceWord()
    {
        _wordIndex = (_wordIndex + 1) % _loop.Length;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task BeginAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            Store.QueueClientEvent("attract_engaged", new { LoopFrame = _loop[_wordIndex].WordKey });
            BeginSessionResponse response = await Api.BeginSessionAsync(
                new BeginSessionRequest(Options.Locale, "touch", Time.GetLocalNow()));
            Store.BeginSession(response, Options.Locale);
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
