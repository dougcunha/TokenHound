using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// HTTP client for querying Claude Code OAuth usage and quota telemetry from Anthropic.
/// </summary>
public sealed class ClaudeOAuthClient
{
    /// <summary>
    /// The default Anthropic OAuth usage endpoint URL.
    /// </summary>
    public const string DEFAULT_USAGE_ENDPOINT = "https://api.anthropic.com/api/oauth/usage";

    /// <summary>
    /// The required beta header name for Anthropic OAuth usage telemetry.
    /// </summary>
    public const string BETA_HEADER_NAME = "anthropic-beta";

    /// <summary>
    /// The required beta header value for Anthropic OAuth usage telemetry.
    /// </summary>
    public const string BETA_HEADER_VALUE = "oauth-2025-04-20";

    /// <summary>
    /// The default user agent header value sent with OAuth usage requests.
    /// </summary>
    public const string USER_AGENT_VALUE = "TokenHound/1.0";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeOAuthClient"/> class with default client and endpoint.
    /// </summary>
    public ClaudeOAuthClient()
        : this(new HttpClient(), null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeOAuthClient"/> class with the specified HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for network requests.</param>
    public ClaudeOAuthClient(HttpClient httpClient)
        : this(httpClient, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeOAuthClient"/> class with the specified HTTP client and endpoint URI.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for network requests.</param>
    /// <param name="endpoint">The custom endpoint URI, or <see langword="null"/> to use <see cref="DEFAULT_USAGE_ENDPOINT"/>.</param>
    public ClaudeOAuthClient(HttpClient httpClient, Uri? endpoint)
    {

        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _endpoint = endpoint ?? new Uri(DEFAULT_USAGE_ENDPOINT);
    }

    /// <summary>
    /// Gets the endpoint URI targeted by this client.
    /// </summary>
    public Uri Endpoint
        => _endpoint;

    /// <summary>
    /// Retrieves current quota and usage telemetry from the Anthropic OAuth usage API.
    /// </summary>
    /// <param name="accessToken">The OAuth bearer access token.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The deserialized <see cref="ClaudeUsageResponse"/>, or <see langword="null"/> if empty.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accessToken"/> is null or whitespace.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (e.g., 401 Unauthorized).</exception>
    /// <exception cref="RateLimitException">Thrown when the API returns HTTP 429 Too Many Requests.</exception>
    public async Task<ClaudeUsageResponse?> GetUsageAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        using var request = CreateRequest(accessToken);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        ).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return await DeserializeResponseAsync(response, cancellationToken).ConfigureAwait(false);

        throw CreateExceptionForResponse(response);
    }

    private HttpRequestMessage CreateRequest(string accessToken)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.TryAddWithoutValidation(BETA_HEADER_NAME, BETA_HEADER_VALUE);
        request.Headers.TryAddWithoutValidation("User-Agent", USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static async Task<ClaudeUsageResponse?> DeserializeResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        return await JsonSerializer.DeserializeAsync<ClaudeUsageResponse>(
            stream,
            SERIALIZER_OPTIONS,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static HttpRequestException CreateExceptionForResponse(HttpResponseMessage response)
    {

        var statusCode = response.StatusCode;

        if (statusCode == HttpStatusCode.TooManyRequests)
        {

            var retryAfter = ExtractRetryAfter(response);

            return new RateLimitException(
                "Claude OAuth API rate limit exceeded (HTTP 429).",
                retryAfter
            );
        }

        if (statusCode == HttpStatusCode.Unauthorized)
            return new HttpRequestException("Claude OAuth request unauthorized (HTTP 401).", null, HttpStatusCode.Unauthorized);

        if (statusCode == HttpStatusCode.Forbidden)
            return new HttpRequestException("Claude OAuth request forbidden (HTTP 403).", null, HttpStatusCode.Forbidden);

        return new HttpRequestException(
            $"Claude OAuth request failed with HTTP {(int)statusCode} ({statusCode}).",
            null,
            statusCode
        );
    }

    private static TimeSpan? ExtractRetryAfter(HttpResponseMessage response)
    {

        if (response.Headers.RetryAfter is not null)
        {

            if (response.Headers.RetryAfter.Delta.HasValue)
                return response.Headers.RetryAfter.Delta.Value;

            if (response.Headers.RetryAfter.Date.HasValue)
            {

                var diff = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;

                return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
            }
        }

        if (response.Headers.TryGetValues("Retry-After", out var values))
        {

            var raw = values.FirstOrDefault();

            if (int.TryParse(raw, CultureInfo.InvariantCulture, out var seconds))
                return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    /// <summary>
    /// Represents an HTTP 429 Too Many Requests response from the Claude OAuth API.
    /// </summary>
    public sealed class RateLimitException : HttpRequestException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitException"/> class.
        /// </summary>
        /// <param name="message">The message describing the rate limit error.</param>
        /// <param name="retryAfter">The retry-after delay, if specified in the response headers.</param>
        public RateLimitException(string message, TimeSpan? retryAfter = null)
            : base(message, null, HttpStatusCode.TooManyRequests)
        {

            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Gets the retry-after duration indicated by the server, if any.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Gets the retry-after duration in whole seconds, if any.
        /// </summary>
        public int? RetryAfterSeconds
            => RetryAfter.HasValue ? (int)Math.Ceiling(RetryAfter.Value.TotalSeconds) : null;
    }
}
