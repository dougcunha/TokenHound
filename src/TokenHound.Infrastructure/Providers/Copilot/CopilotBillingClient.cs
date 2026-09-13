using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Sends versioned, gated requests to GitHub Copilot billing and seat endpoints.
/// </summary>
public sealed class CopilotBillingClient : IDisposable
{
    /// <summary>The default GitHub API base address.</summary>
    public const string DEFAULT_BASE_ADDRESS = "https://api.github.com";

    /// <summary>The recommended maximum duration of one billing request.</summary>
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(15);

    /// <summary>The GitHub API version for billing requests.</summary>
    public const string API_VERSION = "2026-03-10";

    /// <summary>The user agent identifying TokenHound to the GitHub API.</summary>
    public const string USER_AGENT_VALUE = "TokenHound/1.0";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly CopilotRequestGate? _gate;
    private readonly Uri _baseAddress;
    private readonly TimeSpan _timeout;
    private readonly bool _disposeClient;

    internal HttpClient HttpClient
        => _httpClient;

    /// <summary>
    /// Initializes a client with optional HTTP client, gate, custom base URI, and timeout.
    /// </summary>
    /// <param name="httpClient">An optional HTTP client, or null to create an owned client.</param>
    /// <param name="gate">An optional request gate for rate-limit serialization.</param>
    /// <param name="baseAddress">An optional base address for tests.</param>
    /// <param name="timeout">An optional per-request timeout.</param>
    public CopilotBillingClient(
        HttpClient? httpClient = null,
        CopilotRequestGate? gate = null,
        Uri? baseAddress = null,
        TimeSpan? timeout = null)
    {

        _gate = gate;
        _baseAddress = baseAddress ?? new Uri(DEFAULT_BASE_ADDRESS);
        _timeout = timeout ?? DEFAULT_TIMEOUT;

        if (_timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        if (httpClient is null)
        {
            _httpClient = new HttpClient();
            _disposeClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
    }

    /// <summary>
    /// Retrieves AI credit usage for the specified scope, owner, and billing period.
    /// </summary>
    public Task<CopilotBillingResponse> GetBillingUsageAsync(
        CopilotBillingScope scope,
        string owner,
        int year,
        int month,
        string accessToken,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var path = BuildUsagePath(
            scope,
            owner,
            year,
            month
        );
        var uri = new Uri(_baseAddress, path);

        return DispatchAsync<CopilotBillingResponse>(uri, accessToken, cancellationToken);
    }

    /// <summary>
    /// Retrieves seat assignments for the specified candidate organization.
    /// </summary>
    public Task<CopilotSeatResponse> GetSeatAssignmentsAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var path = $"orgs/{Uri.EscapeDataString(organization)}/copilot/billing/seats";
        var uri = new Uri(_baseAddress, path);

        return DispatchAsync<CopilotSeatResponse>(uri, accessToken, cancellationToken);
    }

    /// <summary>
    /// Retrieves the authenticated user login from the GitHub user endpoint.
    /// </summary>
    public async Task<string?> GetAuthenticatedUserLoginAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var uri = new Uri(_baseAddress, "user");
        var user = await DispatchAsync<UserDto>(uri, accessToken, cancellationToken).ConfigureAwait(false);

        return user.Login;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _httpClient.Dispose();
    }

    private static string BuildUsagePath(
        CopilotBillingScope scope,
        string owner,
        int year,
        int month)
    {

        var escapedOwner = Uri.EscapeDataString(owner);
        var query = $"year={year.ToString(CultureInfo.InvariantCulture)}&month={month.ToString(CultureInfo.InvariantCulture)}";

        return scope switch
        {
            CopilotBillingScope.Personal => $"users/{escapedOwner}/settings/billing/ai_credit/usage?{query}",
            CopilotBillingScope.Organization => $"organizations/{escapedOwner}/settings/billing/ai_credit/usage?{query}",
            CopilotBillingScope.Enterprise => $"enterprises/{escapedOwner}/settings/billing/ai_credit/usage?{query}",
            _ => throw new ArgumentException($"Unsupported billing scope: {scope}", nameof(scope))
        };
    }

    private async Task<T> DispatchAsync<T>(
        Uri uri,
        string accessToken,
        CancellationToken cancellationToken)
    {

        if (_gate is not null)
            return await _gate.SendAsync(
                ct => SendCoreAsync<T>(uri, accessToken, ct),
                cancellationToken
            ).ConfigureAwait(false);

        return await SendCoreAsync<T>(uri, accessToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendCoreAsync<T>(
        Uri uri,
        string accessToken,
        CancellationToken cancellationToken)
    {

        using var request = CreateRequest(uri, accessToken);
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
                throw CreateApiException(response);

            var content = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            return JsonSerializer.Deserialize<T>(content, JSON_OPTIONS)
                ?? throw new JsonException("Copilot billing response was empty.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {

            throw new CopilotTimeoutException();
        }
    }

    private static HttpRequestMessage CreateRequest(Uri uri, string accessToken)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", API_VERSION);

        return request;
    }

    private static CopilotApiException CreateApiException(HttpResponseMessage response)
    {

        var retryAfterSeconds = HttpRetryAfterParser.ExtractSeconds(response, TimeProvider.System);
        var message = $"Copilot billing request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";

        return new CopilotApiException(message, response.StatusCode, retryAfterSeconds);
    }

    private sealed record UserDto
    {
        [JsonPropertyName("login")]
        public string? Login { get; init; }
    }
}
