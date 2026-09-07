using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Sends one bounded, non-retrying request to the Copilot internal quota endpoint.
/// </summary>
public sealed class CopilotApiClient : IDisposable
{
    /// <summary>The official lightweight Copilot quota endpoint used by this adapter.</summary>
    public const string DEFAULT_ENDPOINT = "https://api.github.com/copilot_internal/user";

    /// <summary>The recommended maximum duration of one quota request.</summary>
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(15);

    /// <summary>The user agent identifying TokenHound to the GitHub API.</summary>
    public const string USER_AGENT_VALUE = "TokenHound/1.0";

    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly TimeSpan _timeout;
    private readonly bool _disposeClient;

    /// <summary>Initializes a client with the default endpoint and timeout.</summary>
    public CopilotApiClient()
        : this(new HttpClient(), null, null, true)
    {
    }

    /// <summary>Initializes a client using a supplied HTTP client.</summary>
    /// <param name="httpClient">The HTTP client used for the request.</param>
    public CopilotApiClient(HttpClient httpClient)
        : this(httpClient, null, null, false)
    {
    }

    /// <summary>Initializes a client using a supplied HTTP client and endpoint.</summary>
    /// <param name="httpClient">The HTTP client used for the request.</param>
    /// <param name="endpoint">A custom endpoint for tests.</param>
    public CopilotApiClient(HttpClient httpClient, Uri endpoint)
        : this(httpClient, endpoint, null, false)
    {
    }

    /// <summary>Initializes a client with injectable endpoint and request timeout.</summary>
    /// <param name="httpClient">The HTTP client used for the request.</param>
    /// <param name="endpoint">A custom endpoint for tests, or null for the official endpoint.</param>
    /// <param name="timeout">A positive per-request timeout, or null for 15 seconds.</param>
    public CopilotApiClient(
        HttpClient httpClient,
        Uri? endpoint,
        TimeSpan? timeout)
        : this(httpClient, endpoint, timeout, false)
    {
    }

    private CopilotApiClient(
        HttpClient httpClient,
        Uri? endpoint,
        TimeSpan? timeout,
        bool disposeClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _endpoint = endpoint ?? new Uri(DEFAULT_ENDPOINT);
        _timeout = timeout ?? DEFAULT_TIMEOUT;
        _disposeClient = disposeClient;

        if (_timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    /// <summary>Initializes a client with an owned HTTP client and custom endpoint.</summary>
    /// <param name="endpoint">A custom endpoint for tests.</param>
    /// <param name="timeout">A positive per-request timeout.</param>
    public CopilotApiClient(Uri endpoint, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        _httpClient = new HttpClient();
        _endpoint = endpoint;
        _timeout = timeout ?? DEFAULT_TIMEOUT;
        _disposeClient = true;

        if (_timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    /// <summary>Gets the endpoint targeted by this client.</summary>
    public Uri Endpoint
        => _endpoint;

    /// <summary>Gets the enforced per-request timeout.</summary>
    public TimeSpan Timeout
        => _timeout;

    /// <summary>
    /// Retrieves the Copilot quota response with Bearer authorization.
    /// </summary>
    /// <param name="accessToken">The borrowed OAuth or token value.</param>
    /// <param name="cancellationToken">A caller cancellation token.</param>
    /// <returns>The deserialized quota response.</returns>
    /// <exception cref="CopilotApiException">Thrown for an HTTP error response.</exception>
    /// <exception cref="CopilotTimeoutException">Thrown for a provider timeout.</exception>
    public async Task<CopilotQuotaResponse> GetQuotaAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var request = CreateRequest(accessToken);
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
                throw CreateExceptionForResponse(response);

            var content = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            return JsonSerializer.Deserialize<CopilotQuotaResponse>(content, JSON_OPTIONS)
                ?? throw new JsonException("Copilot quota response was empty.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CopilotTimeoutException();
        }
    }

    /// <summary>Alias for <see cref="GetQuotaAsync(string, CancellationToken)"/>.</summary>
    /// <param name="accessToken">The borrowed OAuth or token value.</param>
    /// <param name="cancellationToken">A caller cancellation token.</param>
    /// <returns>The deserialized quota response.</returns>
    public Task<CopilotQuotaResponse> GetUsageAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
        => GetQuotaAsync(accessToken, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeClient)
            _httpClient.Dispose();
    }

    private HttpRequestMessage CreateRequest(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static CopilotApiException CreateExceptionForResponse(HttpResponseMessage response)
    {
        var retryAfterSeconds = response.StatusCode == HttpStatusCode.TooManyRequests
            ? ReadRetryAfterSeconds(response)
            : null;
        var message = $"Copilot quota request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";

        return new CopilotApiException(message, response.StatusCode, retryAfterSeconds);
    }

    private static int? ReadRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return (int)Math.Ceiling(delta.TotalSeconds);

        if (response.Headers.RetryAfter?.Date is { } date)
            return (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds);

        if (response.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(
                global::System.Linq.Enumerable.FirstOrDefault(values),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var seconds))
            return seconds;

        return null;
    }

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>Represents an HTTP response error from the Copilot endpoint.</summary>
public sealed class CopilotApiException : HttpRequestException
{
    /// <summary>Initializes an HTTP error without retaining the response body or token.</summary>
    /// <param name="message">A non-sensitive diagnostic.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="retryAfterSeconds">The parsed Retry-After value, if present.</param>
    public CopilotApiException(
        string message,
        HttpStatusCode statusCode,
        int? retryAfterSeconds = null)
        : base(message, null, statusCode)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    /// <summary>Gets the parsed Retry-After value in seconds, if present.</summary>
    public int? RetryAfterSeconds { get; }
}

/// <summary>Represents a request timeout caused by the provider's bound.</summary>
public sealed class CopilotTimeoutException : TimeoutException
{
    /// <summary>Initializes a non-sensitive Copilot timeout exception.</summary>
    public CopilotTimeoutException()
        : base("Copilot quota request timed out.")
    {
    }
}
