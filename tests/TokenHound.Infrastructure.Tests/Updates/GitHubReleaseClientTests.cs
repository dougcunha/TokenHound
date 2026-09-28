using AwesomeAssertions;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies request shape, JSON mapping, and status handling of <see cref="GitHubReleaseClient"/> (TC-09).
/// </summary>
public sealed class GitHubReleaseClientTests
{
    /// <summary>A trimmed <c>releases/latest</c> payload with one digest-carrying asset and one without.</summary>
    internal const string RELEASE_JSON = """
    {
      "tag_name": "v1.4.0",
      "prerelease": false,
      "draft": false,
      "html_url": "https://github.com/dougcunha/TokenHound/releases/tag/v1.4.0",
      "assets": [
        {
          "name": "TokenHound-1.4.0-win-x64-fxdependent.zip",
          "size": 1234,
          "browser_download_url": "https://github.com/dougcunha/TokenHound/releases/download/v1.4.0/TokenHound-1.4.0-win-x64-fxdependent.zip",
          "digest": "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789"
        },
        {
          "name": "TokenHound-Setup-1.4.0-win-x64.exe",
          "size": 5678,
          "browser_download_url": "https://github.com/dougcunha/TokenHound/releases/download/v1.4.0/TokenHound-Setup-1.4.0-win-x64.exe"
        }
      ]
    }
    """;

    /// <summary>
    /// Verifies that a 200 response is mapped to the release model.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_MapsRelease()
    {

        using var client = CreateClient(_ => Json(HttpStatusCode.OK, RELEASE_JSON), out _);

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        release!.TagName.Should().Be("v1.4.0");
        release.IsPrerelease.Should().BeFalse();
        release.HtmlUrl!.AbsoluteUri.Should().Be("https://github.com/dougcunha/TokenHound/releases/tag/v1.4.0");
        release.Assets.Should().HaveCount(2);
        release.Assets[0].Size.Should().Be(1234);
        release.Assets[0].Digest.Should().StartWith("sha256:");
        release.Assets[1].Digest.Should().BeNull();
    }

    /// <summary>
    /// Verifies the prerelease and draft flags are mapped.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_MapsPrereleaseAndDraftFlags()
    {

        var json = """{"tag_name":"v2.0.0-beta.1","prerelease":true,"draft":true,"assets":[]}""";
        using var client = CreateClient(_ => Json(HttpStatusCode.OK, json), out _);

        var release = await client.GetLatestAsync(TestContext.Current.CancellationToken);

        release!.IsPrerelease.Should().BeTrue();
        release.IsDraft.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the GitHub headers are sent and no credentials are.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_SendsGitHubHeadersWithoutAuthorization()
    {

        using var client = CreateClient(_ => Json(HttpStatusCode.OK, RELEASE_JSON), out var handler);

        await client.GetLatestAsync(TestContext.Current.CancellationToken);

        var request = handler.Requests.Single();
        request.RequestUri.Should().Be(GitHubReleaseClient.LATEST_RELEASE_URL);
        request.Headers.UserAgent.ToString().Should().Be("TokenHound/1.2.3");
        request.Headers.Accept.ToString().Should().Be("application/vnd.github+json");
        request.Headers.GetValues("X-GitHub-Api-Version").Single().Should().Be("2022-11-28");
        request.Headers.Authorization.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a repository without releases yields no release.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_WhenNotFound_ReturnsNull()
    {

        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out _);

        (await client.GetLatestAsync(TestContext.Current.CancellationToken)).Should().BeNull();
    }

    /// <summary>
    /// Verifies that a server error surfaces as an HTTP failure, not a rate limit.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_WhenServerError_ThrowsHttpRequestException()
    {

        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError), out _);

        var act = () => client.GetLatestAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Verifies that 429 carries the Retry-After seconds.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_When429_ThrowsRateLimitedWithRetryAfter()
    {

        using var client = CreateClient(_ => RateLimited(HttpStatusCode.TooManyRequests, "120", null, null), out _);

        var act = () => client.GetLatestAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<UpdateRateLimitedException>()).Which.RetryAfterSeconds.Should().Be(120);
    }

    /// <summary>
    /// Verifies that 403 with an exhausted quota carries the reset instant, and 403 otherwise is a plain failure.
    /// </summary>
    [Fact]
    public async Task GetLatestAsync_When403_DistinguishesExhaustedQuota()
    {

        var reset = new DateTimeOffset(2026, 9, 28, 13, 0, 0, TimeSpan.Zero);
        using var limited = CreateClient(_ => RateLimited(HttpStatusCode.Forbidden, null, "0", reset), out _);
        using var forbidden = CreateClient(_ => RateLimited(HttpStatusCode.Forbidden, null, "12", reset), out _);

        var limitedAct = () => limited.GetLatestAsync(TestContext.Current.CancellationToken);
        var forbiddenAct = () => forbidden.GetLatestAsync(TestContext.Current.CancellationToken);

        (await limitedAct.Should().ThrowAsync<UpdateRateLimitedException>()).Which.ResetUtc.Should().Be(reset);
        await forbiddenAct.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// Creates a client over a scripted handler.
    /// </summary>
    /// <param name="script">The response script.</param>
    /// <param name="handler">The handler recording requests.</param>
    /// <returns>A client that owns its <see cref="HttpClient"/>.</returns>
    internal static GitHubReleaseClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> script, out ScriptedHttpHandler handler)
    {

        handler = new ScriptedHttpHandler(script);

        return new GitHubReleaseClient("1.2.3", new HttpClient(handler));
    }

    /// <summary>
    /// Builds a JSON response.
    /// </summary>
    /// <param name="status">The status code.</param>
    /// <param name="json">The body.</param>
    /// <returns>The response.</returns>
    internal static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage RateLimited(HttpStatusCode status, string? retryAfter, string? remaining, DateTimeOffset? reset)
    {

        var response = new HttpResponseMessage(status);

        if (retryAfter is not null)
            response.Headers.Add("Retry-After", retryAfter);

        if (remaining is not null)
            response.Headers.Add("X-RateLimit-Remaining", remaining);

        if (reset is not null)
            response.Headers.Add("X-RateLimit-Reset", reset.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

        return response;
    }
}
