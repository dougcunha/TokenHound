using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// HTTP client for querying the Cline account API with a borrowed access token.
/// </summary>
/// <remarks>
/// The Bearer credential is forwarded exactly as stored by Cline, including the <c>workos:</c> scheme
/// prefix, because the account API rejects a token whose prefix was stripped.
/// </remarks>
public sealed partial class ClineAccountClient : IDisposable
{
    /// <summary>The default Cline account API base URL.</summary>
    public const string DEFAULT_BASE_URL = "https://api.cline.bot";

    /// <summary>The user agent header value identifying TokenHound.</summary>
    public const string USER_AGENT_VALUE = "TokenHound";

    /// <summary>The default request timeout bound (10 seconds).</summary>
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);

    /// <summary>The maximum permissible request timeout bound (10 seconds).</summary>
    public static readonly TimeSpan MAX_TIMEOUT = TimeSpan.FromSeconds(10);

    /// <summary>The account identity endpoint path.</summary>
    public const string USERS_ME_PATH = "/api/v1/users/me";

    /// <summary>The active plan endpoint path.</summary>
    public const string USERS_ME_PLAN_PATH = "/api/v1/users/me/plan";

    /// <summary>The user credit balance endpoint path template.</summary>
    public const string USER_BALANCE_PATH_TEMPLATE = "/api/v1/users/{0}/balance";

    /// <summary>The user usage transaction endpoint path template.</summary>
    public const string USER_USAGE_PATH_TEMPLATE = "/api/v1/users/{0}/usages";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineAccountClient"/> class with an optional HTTP message handler.
    /// </summary>
    /// <param name="httpMessageHandler">An optional custom message handler for testing.</param>
    /// <param name="timeout">An optional request timeout, bounded at 10 seconds maximum.</param>
    /// <param name="baseUrl">An optional custom account API base URL for testing.</param>
    /// <param name="timeProvider">An optional time provider for clock operations.</param>
    public ClineAccountClient(
        HttpMessageHandler? httpMessageHandler = null,
        TimeSpan? timeout = null,
        Uri? baseUrl = null,
        TimeProvider? timeProvider = null)
    {

        _timeout = ValidateTimeout(timeout);
        _baseUrl = baseUrl ?? new Uri(DEFAULT_BASE_URL);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _disposeClient = true;

        _httpClient = httpMessageHandler is not null
            ? new HttpClient(httpMessageHandler, disposeHandler: false) { Timeout = _timeout }
            : new HttpClient { Timeout = _timeout };
    }

    /// <summary>
    /// Gets the account API base URL targeted by this client.
    /// </summary>
    public Uri BaseUrl
        => _baseUrl;

    /// <summary>
    /// Gets the enforced per-request timeout duration.
    /// </summary>
    public TimeSpan Timeout
        => _timeout;

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _httpClient.Dispose();
    }

    /// <summary>
    /// Fetches the borrowed Cline account identity.
    /// </summary>
    /// <param name="accessToken">The borrowed access token, including its scheme prefix.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The account identity, or <see langword="null"/> when the payload is empty.</returns>
    /// <exception cref="ClineAuthException">Thrown on HTTP 401 Unauthorized.</exception>
    /// <exception cref="ClineEntitlementException">Thrown on HTTP 403 Forbidden.</exception>
    /// <exception cref="ClineRateLimitException">Thrown on HTTP 429 Too Many Requests.</exception>
    /// <exception cref="ClineTimeoutException">Thrown when the request exceeds the enforced timeout.</exception>
    /// <exception cref="HttpRequestException">Thrown on other non-success HTTP status codes.</exception>
    public Task<ClineAccountUser?> GetUserAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
        => GetDataAsync<ClineAccountUser>(
            accessToken,
            USERS_ME_PATH,
            allowNotFound: false,
            cancellationToken
        );

    /// <summary>
    /// Fetches the Cline credit balance of a user.
    /// </summary>
    /// <param name="accessToken">The borrowed access token, including its scheme prefix.</param>
    /// <param name="userId">The Cline account identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The reported balance, or <see langword="null"/> when the payload is empty.</returns>
    public Task<ClineAccountBalance?> GetBalanceAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return GetDataAsync<ClineAccountBalance>(
            accessToken,
            string.Format(CultureInfo.InvariantCulture, USER_BALANCE_PATH_TEMPLATE, Uri.EscapeDataString(userId)),
            allowNotFound: false,
            cancellationToken
        );
    }

    /// <summary>
    /// Fetches the active Cline subscription plan.
    /// </summary>
    /// <param name="accessToken">The borrowed access token, including its scheme prefix.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>
    /// The active plan, or <see langword="null"/> when the account has no plan history, which the
    /// account API reports as HTTP 404 with an empty payload.
    /// </returns>
    public Task<ClineCurrentPlan?> GetPlanAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
        => GetDataAsync<ClineCurrentPlan>(
            accessToken,
            USERS_ME_PLAN_PATH,
            allowNotFound: true,
            cancellationToken
        );

    /// <summary>
    /// Fetches the newest usage transactions of a user.
    /// </summary>
    /// <param name="accessToken">The borrowed access token, including its scheme prefix.</param>
    /// <param name="userId">The Cline account identifier.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The reported transactions, or an empty list when none were returned.</returns>
    public async Task<IReadOnlyList<ClineUsageTransaction>> GetUsageAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken = default)
    {

        var payload = await GetDataAsync<ClineUsageData>(
            accessToken,
            string.Format(CultureInfo.InvariantCulture, USER_USAGE_PATH_TEMPLATE, Uri.EscapeDataString(userId)),
            allowNotFound: true,
            cancellationToken
        ).ConfigureAwait(false);

        return payload?.Items ?? [];
    }

    private async Task<T?> GetDataAsync<T>(
        string accessToken,
        string path,
        bool allowNotFound,
        CancellationToken cancellationToken)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var request = CreateRequest(accessToken, path);
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

            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
                throw await CreateExceptionForResponseAsync(response, linkedCts.Token).ConfigureAwait(false);

            return await DeserializeAsync<T>(response, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {

            throw new ClineTimeoutException("Cline account request timed out.", ex);
        }
    }

    private HttpRequestMessage CreateRequest(string accessToken, string path)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_baseUrl, path));

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken.Trim()}");
        request.Headers.TryAddWithoutValidation("User-Agent", USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static async Task<T?> DeserializeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
            return default;

        var envelope = JsonSerializer.Deserialize<ClineEnvelope<T>>(content, JSON_OPTIONS);

        return envelope is null || !envelope.Success
            ? default
            : envelope.Data;
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