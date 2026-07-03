using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Done (Obsidian done export): gradient check pop, the QR receipt card rendered from
/// <c>receipt.qr_payload</c>, and the RECEIPT · SCAN OR TAP label. Aborted or rejected
/// sessions get the neutral ended variant. Auto-returns to attract after the configured
/// dwell.
/// </summary>
public partial class DoneScreen : IDisposable
{
    private ITimer? _returnTimer;

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

    private bool IsHappyPath => Store.Receipt is not null && Store.AbortReason is null;

    private string Message
    {
        get
        {
            if (Store.AbortReason is not null)
            {
                return L["done.message.aborted"];
            }

            if (Store.Rejection is not null)
            {
                return L["done.message.returned"];
            }

            return Store.Payout?.Method switch
            {
                "cash" => L["done.message.cash"],
                "bank_transfer" => L["done.message.bank_transfer"],
                "debit_card" => L["done.message.debit_card"],
                _ => Store.Receipt is null ? L["done.message.returned"] : L["done.message.generic"],
            };
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Store.Changed += OnStoreChanged;
        _returnTimer = Time.CreateTimer(
            _ => Flow.CompleteToAttract(),
            null,
            TimeSpan.FromSeconds(Options.DoneScreenSeconds),
            Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        _returnTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);
}
