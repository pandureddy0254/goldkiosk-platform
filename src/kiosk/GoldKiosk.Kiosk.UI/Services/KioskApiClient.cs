using System.Net.Http;
using System.Net.Http.Json;
using GoldKiosk.Contracts.V1.Agent;
using GoldKiosk.Contracts.V1.Contact;
using GoldKiosk.Contracts.V1.Devices;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Rates;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.UI.Logging;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Typed HTTP client for the on-machine Kiosk API (<c>/api/v1</c>, snake_case JSON).
/// Every state-changing call carries a fresh <c>Idempotency-Key</c> header; errors are
/// parsed as RFC 7807 problems and surfaced as <see cref="KioskApiException"/>.
/// </summary>
/// <param name="httpClient">The HTTP client with the Kiosk API base address.</param>
/// <param name="logger">The logger (ids and type codes only — never PII).</param>
public sealed class KioskApiClient(HttpClient httpClient, ILogger<KioskApiClient> logger)
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly IReadOnlyDictionary<string, string> _emptyBody =
        new Dictionary<string, string>();

    /// <summary>Begins a session at the attract-loop tap (<c>POST /sessions</c>).</summary>
    /// <param name="request">The begin-session request.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The new session with feature flags and timeouts.</returns>
    public Task<BeginSessionResponse> BeginSessionAsync(
        BeginSessionRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<BeginSessionResponse>("api/v1/sessions", request, idempotent: true, cancellationToken);

    /// <summary>Gets the full session snapshot for crash/reload resume (<c>GET /sessions/{id}</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The session snapshot.</returns>
    public Task<SessionSnapshotResponse> GetSessionAsync(
        string sessionId, CancellationToken cancellationToken = default) =>
        GetAsync<SessionSnapshotResponse>($"api/v1/sessions/{Uri.EscapeDataString(sessionId)}", cancellationToken);

    /// <summary>Sends clubbed payload #1: tray-open plus everything selected so far.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The tray-open request with setup and batched client events.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The tray state after the command.</returns>
    public Task<TrayStateResponse> OpenTrayAsync(
        string sessionId, TrayOpenRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<TrayStateResponse>(SessionPath(sessionId, "tray/open"), request, idempotent: true, cancellationToken);

    /// <summary>Sends clubbed payload #2: tray-close, has-item flag, batched client events.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The tray-close request.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The tray state after the command.</returns>
    public Task<TrayStateResponse> CloseTrayAsync(
        string sessionId, TrayCloseRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<TrayStateResponse>(SessionPath(sessionId, "tray/close"), request, idempotent: true, cancellationToken);

    /// <summary>Accepts the current offer (<c>POST /sessions/{id}/offer/accept</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> AcceptOfferAsync(
        string sessionId, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "offer/accept"), _emptyBody, idempotent: true, cancellationToken);

    /// <summary>Declines the current offer; the item is returned (<c>POST /sessions/{id}/offer/decline</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> DeclineOfferAsync(
        string sessionId, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "offer/decline"), _emptyBody, idempotent: true, cancellationToken);

    /// <summary>Asks the AI explainer how the offer was calculated.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The optional customer question.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The explanation and disclaimer.</returns>
    public Task<ExplainOfferResponse> ExplainOfferAsync(
        string sessionId, ExplainOfferRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ExplainOfferResponse>(SessionPath(sessionId, "offer/explain"), request, idempotent: false, cancellationToken);

    /// <summary>Starts the hardware-driven identity sequence (<c>POST /sessions/{id}/identity/start</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state with identity progress.</returns>
    public Task<SessionActionResponse> StartIdentityAsync(
        string sessionId, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "identity/start"), _emptyBody, idempotent: true, cancellationToken);

    /// <summary>Submits the on-screen signature clubbed with the signed terms version.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The signature PNG and terms version.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> SubmitSignatureAsync(
        string sessionId, IdentitySignatureRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "identity/signature"), request, idempotent: true, cancellationToken);

    /// <summary>Submits email, phone and receipt channels in one clubbed call.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The contact request.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> SubmitContactAsync(
        string sessionId, ContactRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "contact"), request, idempotent: true, cancellationToken);

    /// <summary>Submits the payout method (and bank details when transferring) in one call.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The payout request.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state with the confirmed payout.</returns>
    public Task<SessionActionResponse> SubmitPayoutAsync(
        string sessionId, PayoutRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "payout"), request, idempotent: true, cancellationToken);

    /// <summary>Settles the session: bag the item and dispense/transfer (<c>POST /sessions/{id}/settle</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> SettleAsync(
        string sessionId, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "settle"), _emptyBody, idempotent: true, cancellationToken);

    /// <summary>Aborts the session (<c>POST /sessions/{id}/abort</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The abort reason and item-return flag.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> AbortSessionAsync(
        string sessionId, AbortSessionRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "abort"), request, idempotent: true, cancellationToken);

    /// <summary>Escalates the item check to a live agent (<c>POST /sessions/{id}/agent/item-check</c>).</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The escalation trigger.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The post-command session state.</returns>
    public Task<SessionActionResponse> RequestAgentItemCheckAsync(
        string sessionId, AgentItemCheckRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SessionActionResponse>(SessionPath(sessionId, "agent/item-check"), request, idempotent: true, cancellationToken);

    /// <summary>Gets the hardware-health snapshot for every device port (<c>GET /devices</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The devices snapshot.</returns>
    public Task<DevicesSnapshotResponse> GetDevicesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<DevicesSnapshotResponse>("api/v1/devices", cancellationToken);

    /// <summary>Runs a single-device diagnostic probe (<c>POST /devices/{key}/probe</c>).</summary>
    /// <param name="deviceKey">The device key, e.g. <c>scale</c>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The probe outcome.</returns>
    public Task<DeviceProbeResponse> ProbeDeviceAsync(
        string deviceKey, CancellationToken cancellationToken = default) =>
        PostAsync<DeviceProbeResponse>(
            $"api/v1/devices/{Uri.EscapeDataString(deviceKey)}/probe", _emptyBody, idempotent: false, cancellationToken);

    /// <summary>Gets the display-safe market ticker for the attract loop (<c>GET /rates</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The per-metal display rates.</returns>
    public Task<RatesResponse> GetRatesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<RatesResponse>("api/v1/rates", cancellationToken);

    private static string SessionPath(string sessionId, string suffix) =>
        $"api/v1/sessions/{Uri.EscapeDataString(sessionId)}/{suffix}";

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        return await SendAsync<T>(request, cancellationToken);
    }

    private async Task<T> PostAsync<T>(
        string path, object body, bool idempotent, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, body.GetType(), options: KioskJson.Options),
        };

        if (idempotent)
        {
            request.Headers.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString("N"));
        }

        return await SendAsync<T>(request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            KioskProblem? problem = null;
            try
            {
                problem = await response.Content.ReadFromJsonAsync<KioskProblem>(KioskJson.Options, cancellationToken);
            }
            catch (System.Text.Json.JsonException)
            {
                // Non-JSON error body (proxy page, empty body); surface the status alone.
            }

            logger.ApiProblem(
                request.Method.Method, request.RequestUri?.ToString(), (int)response.StatusCode, problem?.Code ?? "none");
            throw new KioskApiException(response.StatusCode, problem);
        }

        T? payload = await response.Content.ReadFromJsonAsync<T>(KioskJson.Options, cancellationToken);
        return payload is not null
            ? payload
            : throw new KioskApiException($"Kiosk API returned an empty body for {request.RequestUri}.");
    }
}
