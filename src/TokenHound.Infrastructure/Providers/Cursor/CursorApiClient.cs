using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// HTTP client for fetching allowance telemetry from Cursor's usage summary endpoint.
/// </summary>
public sealed class CursorApiClient : IDisposable
{
    private const string USAGE_URL = "https://cursor.com/api/usage-summary";
    private const string COOKIE_HEADER_NAME = "Cookie";
    private const string USER_AGENT_VALUE = "TokenHound/1.0";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">Optional pre-configured HttpClient instance.</param>
    public CursorApiClient(HttpClient? httpClient = null)
    {

        if (httpClient is not null)
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _disposeClient = true;
        }
    }

    /// <summary>
    /// Fetches the usage summary using the WorkosCursorSessionToken cookie format.
    /// </summary>
    /// <param name="stripeMembershipAuthId">The user's WorkOS membership identifier.</param>
    /// <param name="accessToken">The user's JWT access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized usage summary.</returns>
    /// <exception cref="ProviderHttpException">Thrown when the endpoint returns a non-success status.</exception>
    public async ValueTask<CursorUsageResponse> GetUsageSummaryAsync(
        string stripeMembershipAuthId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeMembershipAuthId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        using var request = CreateRequest(stripeMembershipAuthId, accessToken);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        ).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var message = $"Cursor usage request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";

            throw ProviderHttpException.FromResponse(response, message);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Deserialize<CursorUsageResponse>(content, JSON_OPTIONS)
            ?? throw new JsonException("Cursor usage response was empty.");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }

    private static HttpRequestMessage CreateRequest(
        string stripeMembershipAuthId,
        string accessToken)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, USAGE_URL);
        var cookieValue = $"WorkosCursorSessionToken={stripeMembershipAuthId}::{accessToken}";

        request.Headers.Add(COOKIE_HEADER_NAME, cookieValue);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);

        return request;
    }
}
