using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    /// <returns>The deserialized usage summary, or null on error or unauthorized.</returns>
    public async ValueTask<CursorUsageResponse?> GetUsageSummaryAsync(
        string stripeMembershipAuthId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeMembershipAuthId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var cookieValue = $"WorkosCursorSessionToken={stripeMembershipAuthId}::{accessToken}";

        using var request = new HttpRequestMessage(HttpMethod.Get, USAGE_URL);
        request.Headers.Add(COOKIE_HEADER_NAME, cookieValue);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<CursorUsageResponse>(content, JSON_OPTIONS);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}
