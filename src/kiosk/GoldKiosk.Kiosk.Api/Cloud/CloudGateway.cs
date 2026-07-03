using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoldKiosk.Contracts.V1.Cloud.Auth;
using GoldKiosk.Contracts.V1.Cloud.Kiosk;
using GoldKiosk.Contracts.V1.Cloud.Offers;
using GoldKiosk.Contracts.V1.Cloud.Transactions;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.Core.Cloud;
using GoldKiosk.Kiosk.Core.Persistence;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// The typed-HttpClient <see cref="ICloudGateway"/>: a single long-lived instance that owns
/// the kiosk bearer token (Code + PIN → JWT, cached and refreshed on <c>401</c>), speaks the
/// snake_case cloud wire, stamps the mandatory <c>Idempotency-Key</c> on transaction uploads,
/// and never throws across the port — an unreachable or erroring cloud yields
/// <see langword="null"/>/<see langword="false"/>. Logs identifiers only (never the PIN,
/// never the token, no PII).
/// </summary>
/// <remarks>
/// Contract gap: Cloud.Api exposes no transaction-image endpoint yet, so image forwarding is
/// gated by <see cref="CloudOptions.UploadImages"/> (off by default) and, when enabled, POSTs
/// to a conventional <c>transactions/{id}/images</c> route. Marking a transaction forwarded is
/// gated on the record POST alone; image forwarding is best-effort until that endpoint lands.
/// </remarks>
public sealed class CloudGateway : ICloudGateway, IDisposable
{
    /// <summary>The name of the configured <see cref="HttpClient"/> this gateway uses.</summary>
    public const string HttpClientName = "cloud-api";

    private const string DefaultCategory = "other";
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan _tokenRefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<CloudOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CloudGateway> _logger;

    // Token cache guarded by the auth gate so concurrent callers log in at most once.
    private readonly SemaphoreSlim _authGate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt;
    private Guid? _tenantId;

    /// <summary>Initializes the gateway.</summary>
    /// <param name="httpClientFactory">Factory for the configured cloud <see cref="HttpClient"/>.</param>
    /// <param name="options">The validated cloud options.</param>
    /// <param name="timeProvider">The clock (token-expiry checks).</param>
    /// <param name="logger">The host logger.</param>
    public CloudGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<CloudOptions> options,
        TimeProvider timeProvider,
        ILogger<CloudGateway> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<KioskProvisioning?> GetProvisioningAsync(CancellationToken cancellationToken = default)
    {
        string? token = await EnsureTokenAsync(cancellationToken);
        if (token is null)
        {
            return null;
        }

        KioskConfigResponse? config =
            await GetJsonAsync<KioskConfigResponse>("api/v1/kiosk/config", "kiosk-config", cancellationToken);
        if (config is null)
        {
            return null;
        }

        KioskStatusResponse? status =
            await GetJsonAsync<KioskStatusResponse>("api/v1/kiosk/status", "kiosk-status", cancellationToken);
        if (status is null)
        {
            return null;
        }

        return new KioskProvisioning(
            _tenantId ?? Guid.Empty,
            config.KioskId,
            status.CanTrade,
            OfferPercent: 0m,
            config.Currency,
            new KaratRange(config.KaratRange.MinKarat, config.KaratRange.MaxKarat));
    }

    /// <inheritdoc />
    public async Task<CloudOffer?> GetOfferAsync(CloudOfferInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var wire = new CloudOfferRequest(
            Metal: input.MetalType,
            Karat: DeriveKarat(input.GoldPercent),
            PurityPercent: Math.Clamp(input.GoldPercent, 0.01m, 100m),
            WeightGrams: input.WeightGrams,
            Category: DefaultCategory,
            Kind: input.Kind);

        using HttpResponseMessage? response = await SendAuthedAsync(
            () => JsonRequest(HttpMethod.Post, "api/v1/offers", wire), "offer", cancellationToken);
        if (response is null)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.CloudRequestFailed("offer", response.StatusCode);
            return null;
        }

        CloudOfferResponse? body = await ReadJsonAsync<CloudOfferResponse>(response, "offer", cancellationToken);
        if (body is null)
        {
            return null;
        }

        return new CloudOffer(
            body.Amount.AmountMinor,
            body.Amount.Currency,
            body.Amount.Display,
            body.OfferId.ToString("D"),
            body.LockedUntil,
            PawnTerms: null);
    }

    /// <inheritdoc />
    public async Task<bool> UploadTransactionAsync(
        TransactionUpload upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        if (!TryBuildRecord(upload, out RecordTransactionRequest? record) || record is null)
        {
            // Not a completed, uploadable transaction (defence in depth; the worker also
            // filters). Report done so the outbox does not retry a non-transaction folder.
            _logger.TransactionSkipped(upload.SessionId, "not an uploadable completed transaction");
            return true;
        }

        string idempotencyKey = record.TransactionId.ToString("D");
        using HttpResponseMessage? response = await SendAuthedAsync(
            () =>
            {
                HttpRequestMessage request = JsonRequest(HttpMethod.Post, "api/v1/transactions", record);
                request.Headers.TryAddWithoutValidation(IdempotencyKeyHeader, idempotencyKey);
                return request;
            },
            "transaction",
            cancellationToken);

        if (response is null)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.CloudRequestFailed("transaction", response.StatusCode);
            return false;
        }

        // Record accepted. Images are best-effort and gated off until Cloud.Api exposes a
        // transaction-image endpoint — a failure there never blocks the record from being
        // marked forwarded.
        if (_options.Value.UploadImages && upload.ImagePaths.Count > 0)
        {
            await TryUploadImagesAsync(record.TransactionId, upload.ImagePaths, cancellationToken);
        }

        _logger.TransactionUploaded(upload.SessionId);
        return true;
    }

    private async Task TryUploadImagesAsync(
        Guid transactionId, IReadOnlyList<string> imagePaths, CancellationToken cancellationToken)
    {
        string route = $"api/v1/transactions/{transactionId:D}/images";
        foreach (string path in imagePaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                string fileName = Path.GetFileName(path);
                string contentType = ContentTypeFor(path);

                // Build fresh multipart content per attempt: SendAuthedAsync may invoke the
                // factory twice (login refresh on 401), and each request disposes its content.
                using HttpResponseMessage? response = await SendAuthedAsync(
                    () =>
                    {
                        var multipart = new MultipartFormDataContent();
                        var file = new ByteArrayContent(bytes);
                        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                        multipart.Add(file, "image", fileName);
                        return new HttpRequestMessage(HttpMethod.Post, route) { Content = multipart };
                    },
                    "transaction-image",
                    cancellationToken);
                if (response is null || !response.IsSuccessStatusCode)
                {
                    // Best-effort: no transaction-image endpoint exists yet; stop trying this
                    // batch rather than log per file.
                    return;
                }
            }
            catch (IOException ex)
            {
                _logger.CloudError(ex, "transaction-image");
                return;
            }
        }
    }

    private async Task<string?> EnsureTokenAsync(CancellationToken cancellationToken)
    {
        await _authGate.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_accessToken is not null && now < _tokenExpiresAt - _tokenRefreshSkew)
            {
                return _accessToken;
            }

            return await LoginAsync(cancellationToken);
        }
        finally
        {
            _authGate.Release();
        }
    }

    private async Task<string?> LoginAsync(CancellationToken cancellationToken)
    {
        CloudOptions options = _options.Value;
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        var loginRequest = new KioskLoginRequest(options.KioskCode, options.KioskPin);

        // TrySendAsync owns disposing the request; no using here (double-dispose is harmless
        // but the intent is that the send path disposes what it is given).
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/kiosk-login")
        {
            Content = JsonContent.Create(loginRequest, options: CloudJson.Options),
        };

        using HttpResponseMessage? response = await TrySendAsync(client, request, "kiosk-login", cancellationToken);
        if (response is null)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LoginRejected();
            ClearToken();
            return null;
        }

        KioskLoginResponse? body = await ReadJsonAsync<KioskLoginResponse>(response, "kiosk-login", cancellationToken);
        if (body is null || string.IsNullOrEmpty(body.AccessToken))
        {
            return null;
        }

        _accessToken = body.AccessToken;
        _tokenExpiresAt = body.ExpiresAt;
        _tenantId = JwtClaims.ReadTenantId(body.AccessToken);
        _logger.KioskLoggedIn(body.KioskId);
        return _accessToken;
    }

    private async Task<T?> GetJsonAsync<T>(string route, string operation, CancellationToken cancellationToken)
    {
        using HttpResponseMessage? response = await SendAuthedAsync(
            () => new HttpRequestMessage(HttpMethod.Get, route), operation, cancellationToken);
        if (response is null)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.CloudRequestFailed(operation, response.StatusCode);
            return default;
        }

        return await ReadJsonAsync<T>(response, operation, cancellationToken);
    }

    private async Task<HttpResponseMessage?> SendAuthedAsync(
        Func<HttpRequestMessage> requestFactory, string operation, CancellationToken cancellationToken)
    {
        string? token = await EnsureTokenAsync(cancellationToken);
        if (token is null)
        {
            return null;
        }

        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        HttpRequestMessage first = requestFactory();
        first.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage? response = await TrySendAsync(client, first, operation, cancellationToken);
        if (response is null || response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // Token expired or revoked server-side: refresh once and retry.
        response.Dispose();
        await InvalidateTokenAsync(cancellationToken);
        token = await EnsureTokenAsync(cancellationToken);
        if (token is null)
        {
            return null;
        }

        HttpRequestMessage retry = requestFactory();
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await TrySendAsync(client, retry, operation, cancellationToken);
    }

    private async Task<HttpResponseMessage?> TrySendAsync(
        HttpClient client, HttpRequestMessage request, string operation, CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // HttpClient.Timeout fired (not the caller's token): treat as unreachable.
            _logger.CloudUnreachable(operation);
            return null;
        }
        catch (HttpRequestException)
        {
            _logger.CloudUnreachable(operation);
            return null;
        }
        finally
        {
            request.Dispose();
        }
    }

    private async Task<T?> ReadJsonAsync<T>(
        HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(CloudJson.Options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.CloudError(ex, operation);
            return default;
        }
        catch (NotSupportedException ex)
        {
            _logger.CloudError(ex, operation);
            return default;
        }
        catch (HttpRequestException ex)
        {
            _logger.CloudError(ex, operation);
            return default;
        }
        catch (IOException ex)
        {
            _logger.CloudError(ex, operation);
            return default;
        }
    }

    private async Task InvalidateTokenAsync(CancellationToken cancellationToken)
    {
        await _authGate.WaitAsync(cancellationToken);
        try
        {
            ClearToken();
        }
        finally
        {
            _authGate.Release();
        }
    }

    private void ClearToken()
    {
        _accessToken = null;
        _tokenExpiresAt = default;
    }

    /// <summary>Disposes the auth gate. Called by the container at singleton teardown.</summary>
    public void Dispose() => _authGate.Dispose();

    private static HttpRequestMessage JsonRequest<T>(HttpMethod method, string route, T body) =>
        new(method, route) { Content = JsonContent.Create(body, options: CloudJson.Options) };

    private static bool TryBuildRecord(TransactionUpload upload, out RecordTransactionRequest? record)
    {
        record = null;

        SessionJournal? journal;
        try
        {
            journal = JsonSerializer.Deserialize<SessionJournal>(upload.JournalJson, CloudJson.Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (journal is null
            || !string.Equals(journal.State, SessionStates.Done, StringComparison.Ordinal)
            || journal.Receipt is null
            || journal.Offer is null
            || journal.Offer.Amount.AmountMinor <= 0
            || journal.Analysis is null)
        {
            return false;
        }

        decimal goldPercent = journal.Analysis.GoldPercent ?? 0m;
        decimal silverPercent = journal.Analysis.SilverPercent ?? 0m;
        string metal = goldPercent >= silverPercent ? "gold" : "silver";
        string kind = string.Equals(journal.Setup?.ServiceType, "pawn", StringComparison.Ordinal) ? "pawn" : "sale";
        string category = string.IsNullOrWhiteSpace(journal.Setup?.ItemHint)
            ? DefaultCategory
            : Truncate(journal.Setup!.ItemHint!, 64);
        Guid? offerId = Guid.TryParse(journal.Offer.OfferId, out Guid parsedOffer) ? parsedOffer : null;

        record = new RecordTransactionRequest(
            TransactionId: DeterministicTransactionId(journal.SessionId),
            Kind: kind,
            Metal: metal,
            Category: category,
            Karat: DeriveKarat(goldPercent),
            WeightGrams: Math.Clamp(journal.Analysis.WeightGrams, 0.001m, 10_000m),
            PurityPercent: Math.Clamp(goldPercent, 0m, 100m),
            Amount: journal.Offer.Amount,
            OfferId: offerId);
        return true;
    }

    private static decimal DeriveKarat(decimal goldPercent) =>
        Math.Clamp(Math.Round(goldPercent / 100m * 24m, 1, MidpointRounding.ToEven), 1m, 24m);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static Guid DeterministicTransactionId(string sessionId)
    {
        // A stable, name-derived id from the durable session id: replays from the outbox
        // produce the same TransactionId (and Idempotency-Key), so the cloud dedupes.
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };
}
