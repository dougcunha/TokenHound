using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Dispatches versioned manifest requests and secure, credential-free signed report downloads.
/// </summary>
public sealed class CopilotMetricsClient : IDisposable
{
    /// <summary>The default GitHub API base address.</summary>
    public const string DEFAULT_BASE_ADDRESS = "https://api.github.com";

    /// <summary>The recommended maximum duration of one metrics request.</summary>
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(15);

    /// <summary>The GitHub API version for metrics requests.</summary>
    public const string API_VERSION = "2026-03-10";

    /// <summary>The user agent identifying TokenHound to the GitHub API.</summary>
    public const string USER_AGENT_VALUE = "TokenHound/1.0";

    /// <summary>The maximum number of redirects followed during signed download.</summary>
    public const int MAX_REDIRECTS = 3;

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _manifestClient;
    private readonly HttpClient _downloadClient;
    private readonly CopilotRequestGate? _gate;
    private readonly Uri _baseAddress;
    private readonly TimeSpan _timeout;
    private readonly bool _disposeManifestClient;
    private readonly bool _disposeDownloadClient;

    /// <summary>
    /// Initializes a client with optional HTTP clients, gate, base URI, and timeout.
    /// </summary>
    public CopilotMetricsClient(
        HttpClient? manifestHttpClient = null,
        HttpClient? downloadHttpClient = null,
        CopilotRequestGate? gate = null,
        Uri? baseAddress = null,
        TimeSpan? timeout = null)
    {

        _gate = gate;
        _baseAddress = baseAddress ?? new Uri(DEFAULT_BASE_ADDRESS);
        _timeout = timeout ?? DEFAULT_TIMEOUT;

        if (_timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        _manifestClient = manifestHttpClient ?? new HttpClient();
        _downloadClient = downloadHttpClient ?? CreateDefaultDownloadClient();
        _disposeManifestClient = manifestHttpClient is null;
        _disposeDownloadClient = downloadHttpClient is null;
    }

    /// <summary>
    /// Retrieves the daily metrics report manifest for the specified scope, owner, and day.
    /// </summary>
    public async Task<CopilotMetricsManifest?> GetMetricsManifestAsync(
        CopilotBillingScope scope,
        string owner,
        DateOnly day,
        string accessToken,
        CopilotPassDispatchBudget? budget = null,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        if (budget is not null && !budget.TryAcquire())
            throw new InvalidOperationException("Pass dispatch budget exhausted.");

        var path = BuildManifestPath(scope, owner, day);
        var uri = new Uri(_baseAddress, path);

        return await DispatchManifestAsync(uri, accessToken, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Securely downloads a signed report stream without authorization headers or cookies.
    /// </summary>
    public async Task<Stream> DownloadReportAsync(
        string downloadUrl,
        CopilotPassDispatchBudget? budget = null,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);
        var currentUri = new Uri(downloadUrl, UriKind.Absolute);
        CopilotDownloadUriValidator.Validate(currentUri);

        for (var redirectCount = 0; redirectCount <= MAX_REDIRECTS; redirectCount++)
        {
            if (budget is not null && !budget.TryAcquire())
                throw new InvalidOperationException("Pass dispatch budget exhausted.");

            var response = await DispatchDownloadAsync(currentUri, cancellationToken).ConfigureAwait(false);

            if (CopilotDownloadUriValidator.IsRedirectStatusCode(response.StatusCode))
            {
                currentUri = CopilotDownloadUriValidator.ResolveRedirectUri(currentUri, response);
                response.Dispose();
                CopilotDownloadUriValidator.Validate(currentUri);

                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var ex = CreateApiException(response);
                response.Dispose();

                throw ex;
            }

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new DisposingStream(contentStream, response);
        }

        throw new HttpRequestException($"Exceeded maximum redirects ({MAX_REDIRECTS}).");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeManifestClient)
            _manifestClient.Dispose();

        if (_disposeDownloadClient)
            _downloadClient.Dispose();
    }

    private static HttpClient CreateDefaultDownloadClient()
    {

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        };

        return new HttpClient(handler);
    }

    private static string BuildManifestPath(CopilotBillingScope scope, string owner, DateOnly day)
    {

        var escapedOwner = Uri.EscapeDataString(owner);
        var dayString = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return scope switch
        {
            CopilotBillingScope.Organization => $"orgs/{escapedOwner}/copilot/metrics/reports/users-1-day?day={dayString}",
            CopilotBillingScope.Enterprise => $"enterprises/{escapedOwner}/copilot/metrics/reports/users-1-day?day={dayString}",
            _ => throw new ArgumentException($"Unsupported metrics scope: {scope}", nameof(scope))
        };
    }

    private async Task<CopilotMetricsManifest?> DispatchManifestAsync(
        Uri uri,
        string accessToken,
        CancellationToken cancellationToken)
    {

        if (_gate is not null)
            return await _gate.SendAsync(
                ct => SendManifestCoreAsync(uri, accessToken, ct),
                cancellationToken
            ).ConfigureAwait(false);

        return await SendManifestCoreAsync(uri, accessToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CopilotMetricsManifest?> SendManifestCoreAsync(
        Uri uri,
        string accessToken,
        CancellationToken cancellationToken)
    {

        using var request = CreateManifestRequest(uri, accessToken);
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var response = await _manifestClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token
            ).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
                return null;

            if (!response.IsSuccessStatusCode)
                throw CreateApiException(response);

            var content = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            return JsonSerializer.Deserialize<CopilotMetricsManifest>(content, JSON_OPTIONS);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CopilotTimeoutException();
        }
    }

    private static HttpRequestMessage CreateManifestRequest(Uri uri, string accessToken)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", API_VERSION);

        return request;
    }

    private async Task<HttpResponseMessage> DispatchDownloadAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {

        if (_gate is not null)
            return await _gate.SendAsync(
                ct => SendDownloadCoreAsync(uri, ct),
                cancellationToken
            ).ConfigureAwait(false);

        return await SendDownloadCoreAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendDownloadCoreAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {

        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(USER_AGENT_VALUE);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

        try
        {
            return await _downloadClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            request.Dispose();

            throw new CopilotTimeoutException();
        }
    }

    private static CopilotApiException CreateApiException(HttpResponseMessage response)
    {

        var retryAfterSeconds = CopilotRateLimitExtractor.ExtractRetryAfterSeconds(response, TimeProvider.System);
        var message = $"Copilot metrics request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";

        return new CopilotApiException(message, response.StatusCode, retryAfterSeconds);
    }
}
