using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// HTTP client for querying OpenCode Go quota and limit window telemetry.
/// </summary>
public sealed class OpenCodeApiClient : IDisposable
{
    /// <summary>The default OpenCode Go usage endpoint URL.</summary>
    public const string DEFAULT_ENDPOINT = "https://opencode.ai/zen/go/v1/usage";

    /// <summary>The user agent header value identifying TokenHound.</summary>
    public const string USER_AGENT_VALUE = "TokenHound";

    /// <summary>The default request timeout bound (10 seconds).</summary>
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);

    /// <summary>The maximum permissible request timeout bound (10 seconds).</summary>
    public static readonly TimeSpan MAX_TIMEOUT = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeApiClient"/> class with an optional HTTP message handler.
    /// </summary>
    /// <param name="httpMessageHandler">An optional custom message handler for testing.</param>
    /// <param name="timeout">An optional request timeout, bounded at 10 seconds maximum.</param>
    /// <param name="endpoint">An optional custom endpoint URI for testing.</param>
    /// <param name="timeProvider">An optional time provider for clock operations.</param>
    public OpenCodeApiClient(
        HttpMessageHandler? httpMessageHandler = null,
        TimeSpan? timeout = null,
        Uri? endpoint = null,
        TimeProvider? timeProvider = null)
    {

        _timeout = ValidateTimeout(timeout);
        _endpoint = endpoint ?? new Uri(DEFAULT_ENDPOINT);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _disposeClient = true;

        _httpClient = httpMessageHandler is not null
            ? new HttpClient(httpMessageHandler, disposeHandler: false) { Timeout = _timeout }
            : new HttpClient { Timeout = _timeout };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeApiClient"/> class using a supplied HTTP client.
    /// </summary>
    /// <param name="httpClient">The pre-configured HTTP client.</param>
    /// <param name="timeout">An optional request timeout, bounded at 10 seconds maximum.</param>
    /// <param name="endpoint">An optional custom endpoint URI for testing.</param>
    /// <param name="timeProvider">An optional time provider for clock operations.</param>
    public OpenCodeApiClient(
        HttpClient httpClient,
        TimeSpan? timeout = null,
        Uri? endpoint = null,
        TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(httpClient);

        _timeout = ValidateTimeout(timeout);
        _endpoint = endpoint ?? new Uri(DEFAULT_ENDPOINT);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _httpClient = httpClient;
        _disposeClient = false;
    }

    /// <summary>Gets the telemetry endpoint URI targeted by this client.</summary>
    public Uri Endpoint
        => _endpoint;

    /// <summary>Gets the enforced per-request timeout duration.</summary>
    public TimeSpan Timeout
        => _timeout;

    /// <summary>
    /// Fetches the OpenCode Go quota telemetry using the provided API key.
    /// </summary>
    /// <param name="apiKey">The OpenCode Go Bearer API key.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The deserialized <see cref="OpenCodeUsageResponse"/> payload.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="apiKey"/> is null or whitespace.</exception>
    /// <exception cref="OpenCodeAuthException">Thrown on HTTP 401 Unauthorized.</exception>
    /// <exception cref="OpenCodeEntitlementException">Thrown on HTTP 403 Forbidden.</exception>
    /// <exception cref="OpenCodeRateLimitException">Thrown on HTTP 429 Too Many Requests.</exception>
    /// <exception cref="OpenCodeTimeoutException">Thrown when the request exceeds the enforced timeout.</exception>
    /// <exception cref="HttpRequestException">Thrown on other non-success HTTP status codes.</exception>
    /// <exception cref="JsonException">Thrown when the response payload cannot be parsed or lacks required windows.</exception>
    public async Task<OpenCodeUsageResponse> GetUsageAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        cancellationToken.ThrowIfCancellationRequested();

        using var request = CreateRequest(apiKey);
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        try
        {

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token
            ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw await CreateExceptionForResponseAsync(response, linkedCts.Token).ConfigureAwait(false);

            return await DeserializeResponseAsync(response, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {

            throw new OpenCodeTimeoutException("OpenCode API request timed out.", ex);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _httpClient.Dispose();
    }

    private HttpRequestMessage CreateRequest(string apiKey)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.TryAddWithoutValidation("User-Agent", USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static async Task<OpenCodeUsageResponse> DeserializeResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var result = JsonSerializer.Deserialize<OpenCodeUsageResponse>(content, JSON_OPTIONS)
            ?? throw new JsonException("OpenCode usage response was empty.");

        if (result.Usage is null ||
            result.Usage.Rolling is null ||
            result.Usage.Weekly is null ||
            result.Usage.Monthly is null)
        {

            throw new JsonException("OpenCode usage response missing required limit windows.");
        }

        return result;
    }

    private async Task<HttpRequestException> CreateExceptionForResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        var errorResponse = await TryDeserializeErrorAsync(response, cancellationToken).ConfigureAwait(false);
        var statusCode = response.StatusCode;
        var message = errorResponse?.Error?.Message;

        if (statusCode == HttpStatusCode.TooManyRequests)
            return CreateRateLimitException(response, message, errorResponse);

        if (statusCode == HttpStatusCode.Unauthorized)
            return new OpenCodeAuthException(message ?? "OpenCode API request unauthorized (HTTP 401).", errorResponse);

        if (statusCode == HttpStatusCode.Forbidden)
            return new OpenCodeEntitlementException(
                message ?? "OpenCode Go subscription entitlement required (HTTP 403).",
                errorResponse
            );

        return new HttpRequestException(
            message ?? $"OpenCode API request failed with HTTP {(int)statusCode} ({statusCode}).",
            null,
            statusCode
        );
    }

    private OpenCodeRateLimitException CreateRateLimitException(
        HttpResponseMessage response,
        string? message,
        OpenCodeErrorResponse? errorResponse)
    {

        var retryAfterSeconds = HttpRetryAfterParser.ExtractSeconds(response, _timeProvider);
        var rateLimitMessage = message ?? "OpenCode API rate limit exceeded (HTTP 429).";

        return new OpenCodeRateLimitException(
            rateLimitMessage,
            retryAfterSeconds,
            errorResponse,
            resetTimeUtc: errorResponse?.ResetsAt
        );
    }

    private static async Task<OpenCodeErrorResponse?> TryDeserializeErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        try
        {

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(content))
                return JsonSerializer.Deserialize<OpenCodeErrorResponse>(content, JSON_OPTIONS);
        }
        catch (Exception)
        {

            // Defensive: ignore body deserialization errors on non-success HTTP status
        }

        return null;
    }

    private static TimeSpan ValidateTimeout(TimeSpan? timeout)
    {

        if (!timeout.HasValue)
            return DEFAULT_TIMEOUT;

        if (timeout.Value <= TimeSpan.Zero || timeout.Value > MAX_TIMEOUT)
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Timeout must be greater than zero and at most 10 seconds."
            );

        return timeout.Value;
    }
}
