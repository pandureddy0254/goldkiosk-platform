using System.Net.Http;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Kiosk.UI.Components.Controls;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Payout (Obsidian payout export): selection cards with the cyan-outline selected
/// state; bank fields expand inline for bank transfer; one clubbed <c>POST /payout</c>.
/// A 409 <c>payout.insufficient_cash</c> marks cash unavailable, narrows the cards to
/// the server's <c>available_methods</c>, and routes recovery through the error overlay.
/// </summary>
public partial class PayoutScreen : IDisposable
{
    private static readonly string[] _fallbackMethods = ["cash", "bank_transfer", "debit_card"];

    private readonly HashSet<string> _unavailableMethods = [];
    private string? _method;
    private BankField _activeField = BankField.None;
    private string _accountHolder = string.Empty;
    private string _routingNumber = string.Empty;
    private string _accountNumber = string.Empty;
    private string _accountType = "checking";
    private bool _busy;

    private enum BankField
    {
        None,
        Holder,
        Routing,
        Account,
    }

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private IReadOnlyList<string> Methods =>
        Store.Features?.PayoutMethods is { Count: > 0 } methods ? methods : _fallbackMethods;

    private TouchKeyboardLayout KeyboardLayout =>
        _activeField == BankField.Holder ? TouchKeyboardLayout.Text : TouchKeyboardLayout.Numeric;

    private bool CanContinue => !_busy && _method is not null && (_method != "bank_transfer" || BankDetailsComplete);

    private bool BankDetailsComplete =>
        _accountHolder.Trim().Length >= 2 && _routingNumber.Length >= 6 && _accountNumber.Length >= 4;

    /// <inheritdoc />
    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private static string MaskNumber(string value) =>
        value.Length <= 4 ? value : new string('•', value.Length - 4) + value[^4..];

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);

    private void SelectMethod(string method)
    {
        _method = method;
        if (method != "bank_transfer")
        {
            _activeField = BankField.None;
        }
    }

    private void FocusField(BankField field) => _activeField = field;

    private void SetAccountType(string accountType) => _accountType = accountType;

    private static RenderFragment GetIcon(string method) => builder =>
    {
        string path = method switch
        {
            "cash" => "M3 7h18v10H3zM12 10.4a1.6 1.6 0 1 1 0 3.2 1.6 1.6 0 0 1 0-3.2Z",
            "bank_transfer" => "M3 9.5 12 4l9 5.5M5 10v7m4.6-7v7m4.8-7v7M19 10v7M3 20h18",
            _ => "M3 6h18v12H3zM3 10h18M6 15h4",
        };
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "1.7");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.OpenElement(7, "path");
        builder.AddAttribute(8, "d", path);
        builder.CloseElement();
        builder.CloseElement();
    };

    private void OnKey(string key)
    {
        switch (_activeField)
        {
            case BankField.Holder:
                _accountHolder += key;
                break;
            case BankField.Routing:
                _routingNumber += key;
                break;
            case BankField.Account:
                _accountNumber += key;
                break;
            case BankField.None:
            default:
                break;
        }
    }

    private void OnBackspace()
    {
        switch (_activeField)
        {
            case BankField.Holder when _accountHolder.Length > 0:
                _accountHolder = _accountHolder[..^1];
                break;
            case BankField.Routing when _routingNumber.Length > 0:
                _routingNumber = _routingNumber[..^1];
                break;
            case BankField.Account when _accountNumber.Length > 0:
                _accountNumber = _accountNumber[..^1];
                break;
            case BankField.None:
            default:
                break;
        }
    }

    private async Task ContinueAsync()
    {
        if (!CanContinue || Store.SessionId is not { } sessionId || _method is not { } method)
        {
            return;
        }

        _busy = true;
        try
        {
            BankDetailsDto? bank = method == "bank_transfer"
                ? new BankDetailsDto(_accountHolder.Trim(), _routingNumber, _accountNumber, _accountType)
                : null;
            SessionActionResponse response = await Api.SubmitPayoutAsync(sessionId, new PayoutRequest(method, bank));
            Store.ApplyAction(response);
        }
        catch (KioskApiException ex) when (ex.Problem?.Type == ProblemTypes.PayoutInsufficientCash)
        {
            // Recovery per the ProblemDetails: mark cash out, narrow to available_methods.
            _unavailableMethods.Add("cash");
            IReadOnlyList<string> available = ex.Problem.GetStringList("available_methods");
            foreach (string method2 in Methods)
            {
                if (available.Count > 0 && !available.Contains(method2, StringComparer.Ordinal))
                {
                    _unavailableMethods.Add(method2);
                }
            }

            _method = null;
            Flow.ShowApiError(ex);
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
