using System.Net.Http;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Kiosk.UI.Components.Controls;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Identity (Obsidian identity export): starts the hardware-driven sequence once, then
/// renders the scanline card and the checklist — ID scanned → Face match →
/// (Fingerprint when required) → Signature. The signature step swaps to a full-screen
/// draw canvas (pointer events via JS interop) and posts the PNG clubbed with the
/// signed terms version.
/// </summary>
public partial class IdentityScreen : IDisposable
{
    private const string CanvasId = "signature-canvas";

    private bool _busy;
    private bool _canvasInitialized;

    [Inject]
    private KioskApiClient Api { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private IJSRuntime Js { get; set; } = default!; // non-null via [Inject]

    private bool ShowSignature => Store.CurrentIdentityStep == "signature";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Store.Changed += OnStoreChanged;

        if (!Store.IdentityStarted && Store.SessionId is { } sessionId)
        {
            Store.MarkIdentityStarted();
            try
            {
                SessionActionResponse response = await Api.StartIdentityAsync(sessionId);
                Store.ApplyAction(response);
            }
            catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
            {
                Flow.ShowApiError(ex);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ShowSignature && !_canvasInitialized)
        {
            _canvasInitialized = true;
            await Js.InvokeVoidAsync("kioskInterop.signature.init", CanvasId);
        }
        else if (!ShowSignature)
        {
            _canvasInitialized = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private static ChecklistState StateFor(IdentityStepDto step) => step.Status switch
    {
        "completed" => ChecklistState.Done,
        "in_progress" => ChecklistState.Active,
        "failed" => ChecklistState.Failed,
        _ => ChecklistState.Pending,
    };

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task ClearSignatureAsync() =>
        await Js.InvokeVoidAsync("kioskInterop.signature.clear", CanvasId);

    private async Task SubmitSignatureAsync()
    {
        if (_busy || Store.SessionId is not { } sessionId)
        {
            return;
        }

        _busy = true;
        try
        {
            bool isEmpty = await Js.InvokeAsync<bool>("kioskInterop.signature.isEmpty", CanvasId);
            if (isEmpty)
            {
                return;
            }

            string dataUrl = await Js.InvokeAsync<string>("kioskInterop.signature.toDataUrl", CanvasId);
            const string prefix = "data:image/png;base64,";
            string base64 = dataUrl.StartsWith(prefix, StringComparison.Ordinal) ? dataUrl[prefix.Length..] : dataUrl;

            SessionActionResponse response = await Api.SubmitSignatureAsync(
                sessionId, new IdentitySignatureRequest(base64, Store.TermsVersion));
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
