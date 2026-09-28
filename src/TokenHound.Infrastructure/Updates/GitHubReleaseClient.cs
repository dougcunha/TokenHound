using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Unauthenticated client for the latest published release of the TokenHound GitHub repository.
/// </summary>
public sealed class GitHubReleaseClient : IDisposable
{
    /// <summary>
    /// The <c>owner/name</c> of the repository that publishes TokenHound releases.
    /// </summary>
    public const string REPOSITORY = "dougcunha/TokenHound";

    /// <summary>
    /// The endpoint returning the latest published, non-prerelease release.
    /// </summary>
    public static readonly Uri LATEST_RELEASE_URL = new($"https://api.github.com/repos/{REPOSITORY}/releases/latest");

    private const string ACCEPT_MEDIA_TYPE = "application/vnd.github+json";
    private const string API_VERSION_HEADER = "X-GitHub-Api-Version";
    private const string API_VERSION = "2022-11-28";
    private const string REMAINING_HEADER = "X-RateLimit-Remaining";
    private const string RESET_HEADER = "X-RateLimit-Reset";

    private static readonly TimeSpan REQUEST_TIMEOUT = TimeSpan.FromSeconds(15);

    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly string _userAgent;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubReleaseClient"/> class.
    /// </summary>
    /// <param name="userAgentVersion">The running version sent in the <c>User-Agent</c> header GitHub requires.</param>
    /// <param name="httpClient">An optional HTTP client; a client with a 15-second timeout is created when omitted.</param>
    /// <param name="timeProvider">An optional clock used to resolve date-based <c>Retry-After</c> values.</param>
    public GitHubReleaseClient(string userAgentVersion, HttpClient? httpClient = null, TimeProvider? timeProvider = null)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(userAgentVersion);

        _userAgent = $"TokenHound/{userAgentVersion}";
        _timeProvider = timeProvider ?? TimeProvider.System;
        _disposeClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient { Timeout = REQUEST_TIMEOUT };
    }

    /// <summary>
    /// Fetches the latest published release.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the request.</param>
    /// <returns>The latest release, or <see langword="null"/> when the repository has no published release (404).</returns>
    /// <exception cref="UpdateRateLimitedException">GitHub answered 429, or 403 with an exhausted quota.</exception>
    /// <exception cref="HttpRequestException">GitHub answered with another non-success status or the network failed.</exception>
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken = default)
    {

        using var request = CreateRequest();
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (IsRateLimited(response))
            throw new UpdateRateLimitedException(HttpRetryAfterParser.ExtractSeconds(response, _timeProvider), ReadReset(response));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReleasePayload>(cancellationToken).ConfigureAwait(false);

        return payload is null ? null : Map(payload);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _httpClient.Dispose();
    }

    private HttpRequestMessage CreateRequest()
    {

        var request = new HttpRequestMessage(HttpMethod.Get, LATEST_RELEASE_URL);

        request.Headers.UserAgent.ParseAdd(_userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ACCEPT_MEDIA_TYPE));
        request.Headers.Add(API_VERSION_HEADER, API_VERSION);

        return request;
    }

    private static bool IsRateLimited(HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.TooManyRequests
            || (response.StatusCode == HttpStatusCode.Forbidden && ReadHeader(response, REMAINING_HEADER) == "0");

    private static DateTimeOffset? ReadReset(HttpResponseMessage response)
        => long.TryParse(
            ReadHeader(response, RESET_HEADER),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var epochSeconds
        ) ? DateTimeOffset.FromUnixTimeSeconds(epochSeconds) : null;

    private static string? ReadHeader(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault()?.Trim() : null;

    private static ReleaseInfo Map(ReleasePayload payload)
        => new()
        {
            TagName = payload.TagName ?? string.Empty,
            IsPrerelease = payload.Prerelease,
            IsDraft = payload.Draft,
            HtmlUrl = Uri.TryCreate(payload.HtmlUrl, UriKind.Absolute, out var htmlUrl) ? htmlUrl : null,
            Assets = [.. (payload.Assets ?? []).Select(MapAsset).OfType<ReleaseAsset>()]
        };

    private static ReleaseAsset? MapAsset(AssetPayload asset)
    {

        if (string.IsNullOrWhiteSpace(asset.Name) || !Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var url))
            return null;

        return new ReleaseAsset { Name = asset.Name, Size = asset.Size, DownloadUrl = url, Digest = asset.Digest };
    }

    private sealed record ReleasePayload
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("draft")]
        public bool Draft { get; init; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("assets")]
        public AssetPayload[]? Assets { get; init; }
    }

    private sealed record AssetPayload
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}
