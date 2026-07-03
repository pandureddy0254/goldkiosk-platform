using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.TestKit;

namespace GoldKiosk.Kiosk.Api.Tests.Support;

/// <summary>
/// Wire-level helpers and flow drivers for the Kiosk API integration tests. Every
/// state-changing POST carries a fresh <c>Idempotency-Key</c> unless the caller pins one.
/// Drivers are guards — they throw on unexpected responses instead of asserting.
/// </summary>
public static class KioskApi
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(15);

    public static string SessionPath(string sessionId, string suffix = "") =>
        $"/api/v1/sessions/{sessionId}{suffix}";

    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, string path, object body, string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: WireJson.Options),
        };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> PostKeyedAsync(HttpClient client, string path, object body) =>
        PostAsync(client, path, body, Guid.NewGuid().ToString());

    public static async Task<HttpResponseMessage> PostExpectingAsync(
        HttpClient client, string path, object body, HttpStatusCode expected)
    {
        HttpResponseMessage response = await PostKeyedAsync(client, path, body);
        if (response.StatusCode != expected)
        {
            string detail = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"POST {path} returned {(int)response.StatusCode} (expected {(int)expected}): {detail}");
        }

        return response;
    }

    public static async Task<BeginSessionResponse> BeginSessionAsync(HttpClient client)
    {
        HttpResponseMessage response = await PostAsync(client, "/api/v1/sessions", RequestMother.BeginSession(), null);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            string detail = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"POST /api/v1/sessions returned {(int)response.StatusCode}: {detail}");
        }

        return (await response.Content.ReadFromJsonAsync<BeginSessionResponse>(WireJson.Options))!;
    }

    public static async Task<SessionSnapshotResponse> GetSnapshotAsync(HttpClient client, string sessionId) =>
        (await client.GetFromJsonAsync<SessionSnapshotResponse>(SessionPath(sessionId), WireJson.Options))!;

    public static async Task<SessionSnapshotResponse> PollSnapshotUntilAsync(
        HttpClient client,
        string sessionId,
        Func<SessionSnapshotResponse, bool> accept,
        string description)
    {
        var stopwatch = Stopwatch.StartNew();
        SessionSnapshotResponse snapshot = await GetSnapshotAsync(client, sessionId);
        while (!accept(snapshot))
        {
            if (stopwatch.Elapsed > PollTimeout)
            {
                throw new TimeoutException(
                    $"Session {sessionId} never reached '{description}'; last state '{snapshot.State}'.");
            }

            await Task.Delay(25);
            snapshot = await GetSnapshotAsync(client, sessionId);
        }

        return snapshot;
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        string raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }

    /// <summary>Begins a session and drives it through the tray to the presented offer.</summary>
    public static async Task<string> DriveToOfferAsync(HttpClient client)
    {
        BeginSessionResponse begun = await BeginSessionAsync(client);
        await PostExpectingAsync(
            client, SessionPath(begun.SessionId, "/tray/open"), RequestMother.TrayOpen(), HttpStatusCode.Accepted);
        await PostExpectingAsync(
            client, SessionPath(begun.SessionId, "/tray/close"), RequestMother.TrayClose(hasItem: true), HttpStatusCode.Accepted);
        await PollSnapshotUntilAsync(
            client, begun.SessionId, s => s.State == SessionStates.Offer, "the presented offer");
        return begun.SessionId;
    }

    /// <summary>Drives a session through offer acceptance and identity to payout selection.</summary>
    public static async Task<string> DriveToPayoutAsync(HttpClient client)
    {
        string sessionId = await DriveToOfferAsync(client);
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/offer/accept"), new { }, HttpStatusCode.Accepted);
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/identity/start"), new { }, HttpStatusCode.Accepted);
        await PollSnapshotUntilAsync(
            client, sessionId, s => s.Identity?.CurrentStep == "signature", "the signature step");
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/identity/signature"), RequestMother.Signature(), HttpStatusCode.Accepted);
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/contact"), RequestMother.Contact(), HttpStatusCode.Accepted);
        return sessionId;
    }

    /// <summary>Drives a session all the way into settlement (cash payout confirmed, settle accepted).</summary>
    public static async Task<string> DriveToSettlingAsync(HttpClient client)
    {
        string sessionId = await DriveToPayoutAsync(client);
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/payout"), RequestMother.CashPayout(), HttpStatusCode.Accepted);
        await PostExpectingAsync(
            client, SessionPath(sessionId, "/settle"), new { }, HttpStatusCode.Accepted);
        return sessionId;
    }
}
