using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Settlement in motion, driven by <c>settlement_progress</c>: Securing your item →
/// Dispensing/Transferring (with the bill mix for cash). The robotic motion is an
/// animated arm-path SVG with a moving dot — never a spinner. Also renders the
/// returning-item state on declines/aborts.
/// </summary>
public partial class ProcessingScreen : IDisposable
{
    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private string Title
    {
        get
        {
            if (Store.State == SessionStates.ReturningItem)
            {
                return L["processing.stage.returning"];
            }

            return Store.SettlementStage switch
            {
                "dispensing" => L["processing.stage.dispensing"],
                "transferring" => L["processing.stage.transferring"],
                _ => L["processing.stage.bagging"],
            };
        }
    }

    private int StageIndex => Store.SettlementStage is "dispensing" or "transferring" ? 1 : 0;

    private int? BillCount =>
        Store.SettlementStage == "dispensing" && Store.SettlementBills is { Count: > 0 } bills
            ? bills.Values.Sum()
            : null;

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
