using System.Net.Http;
using GoldKiosk.Contracts.V1.Contact;
using GoldKiosk.Kiosk.UI.Components.Controls;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Contact and receipt channels on one screen: email and phone entered through the
/// on-screen touch keyboard (never the OS keyboard), QR always on, email/SMS optional.
/// One clubbed <c>POST /contact</c> call.
/// </summary>
public partial class ContactScreen
{
    private Field _activeField = Field.None;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private bool _emailChannel;
    private bool _smsChannel;
    private bool _busy;

    private enum Field
    {
        None,
        Email,
        Phone,
    }

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private TouchKeyboardLayout KeyboardLayout =>
        _activeField == Field.Phone ? TouchKeyboardLayout.Numeric : TouchKeyboardLayout.Email;

    private bool CanContinue =>
        !_busy
        && (!_emailChannel || IsPlausibleEmail(_email))
        && (!_smsChannel || _phone.Length >= 7);

    private static bool IsPlausibleEmail(string email) =>
        email.Length >= 5 && email.Contains('@', StringComparison.Ordinal) && email.Contains('.', StringComparison.Ordinal);

    private void FocusField(Field field) => _activeField = field;

    private void ToggleEmailChannel() => _emailChannel = !_emailChannel;

    private void ToggleSmsChannel() => _smsChannel = !_smsChannel;

    private void OnKey(string key)
    {
        if (_activeField == Field.Email)
        {
            _email += key;
            _emailChannel = true;
        }
        else if (_activeField == Field.Phone)
        {
            _phone += key;
            _smsChannel = true;
        }
    }

    private void OnBackspace()
    {
        if (_activeField == Field.Email && _email.Length > 0)
        {
            _email = _email[..^1];
        }
        else if (_activeField == Field.Phone && _phone.Length > 0)
        {
            _phone = _phone[..^1];
        }
    }

    private async Task ContinueAsync()
    {
        List<string> channels = ["qr"];
        if (_emailChannel)
        {
            channels.Add("email");
        }

        if (_smsChannel)
        {
            channels.Add("sms");
        }

        await SubmitAsync(new ContactRequest(
            _email.Length > 0 ? _email : null,
            _phone.Length > 0 ? _phone : null,
            channels));
    }

    private async Task SkipAsync() =>
        await SubmitAsync(new ContactRequest(Email: null, Phone: null, ["qr"]));

    private async Task SubmitAsync(ContactRequest request)
    {
        if (_busy || Store.SessionId is not { } sessionId)
        {
            return;
        }

        _busy = true;
        try
        {
            SessionActionResponse response = await Api.SubmitContactAsync(sessionId, request);
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
}
