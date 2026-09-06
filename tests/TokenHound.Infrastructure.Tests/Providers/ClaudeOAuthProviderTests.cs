using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies ClaudeOAuthProvider snapshot generation across authenticated, unauthenticated, rate-limited, and failure states.
/// </summary>
public sealed class ClaudeOAuthProviderTests
{
    private const string VALID_JSON = """{"claudeAiOauth":{"accessToken":"sk-valid","expiresAt":253402300799000}}""";
    private const string EXPIRED_JSON = """{"claudeAiOauth":{"accessToken":"sk-expired","expiresAt":1000000000000}}""";
    private const string USAGE_JSON = """{"five_hour":{"utilization":0.35,"resets_at":"2026-08-28T18:00:00Z"},"seven_day":{"utilization":0.72,"resets_at":"2026-09-01T00:00:00Z"}}""";

    /// <summary>
    /// Verifies that ProviderId returns "claude".
    /// </summary>
    [Fact]
    public void ProviderId_ReturnsClaude()
    {

        var provider = new ClaudeOAuthProvider();

        provider.ProviderId.Should().Be("claude");
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns Ok snapshot with mapped five_hour and seven_day limit windows.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenAuthenticatedWithTelemetry_ReturnsOkWithTwoLimitWindows()
    {

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(USAGE_JSON, Encoding.UTF8, "application/json")
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().HaveCount(2);
        snapshot.LimitWindows[0].Name.Should().Be("five_hour");
        snapshot.LimitWindows[0].UsedFraction.Should().Be(0.35);
        snapshot.LimitWindows[0].Period.Should().Be(TimeSpan.FromHours(5));
        snapshot.LimitWindows[1].Name.Should().Be("seven_day");
        snapshot.LimitWindows[1].UsedFraction.Should().Be(0.72);
        snapshot.LimitWindows[1].Period.Should().Be(TimeSpan.FromDays(7));
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns NeedsAuth snapshot when credentials file is missing.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCredentialsMissing_ReturnsNeedsAuth()
    {

        using var scope = new TempProfileScope(null);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var provider = new ClaudeOAuthProvider(discovery);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ErrorDescription.Should().Be("Execute 'claude login' in terminal");
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns NeedsAuth snapshot when access token has expired.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCredentialsExpired_ReturnsNeedsAuth()
    {

        using var scope = new TempProfileScope(EXPIRED_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var provider = new ClaudeOAuthProvider(discovery);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ErrorDescription.Should().Be("Execute 'claude login' in terminal");
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns RateLimited snapshot with ActiveBlock when HTTP 429 occurs.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenRateLimited429WithRetryAfter_ReturnsRateLimitedWithActiveBlock()
    {

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ =>
        {

            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "120");

            return response;
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock.Should().NotBeNull();
        snapshot.ActiveBlock!.IsBlocked.Should().BeTrue();
        snapshot.ActiveBlock.RetryAfterSeconds.Should().Be(120);
        snapshot.ActiveBlock.ResetTimeUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync enforces minimum retry floor when 429 response omits Retry-After header.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenRateLimited429WithoutRetryAfter_ReturnsRateLimitedWithFloor()
    {

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock.Should().NotBeNull();
        snapshot.ActiveBlock!.IsBlocked.Should().BeTrue();
        snapshot.ActiveBlock.RetryAfterSeconds.Should().BeNull();
        snapshot.ActiveBlock.ResetTimeUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync propagates cancellation when token is canceled.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        var provider = new ClaudeOAuthProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await provider.GetSnapshotAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns NeedsAuth snapshot when API returns HTTP 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenUnauthorized401_ReturnsNeedsAuth()
    {

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.ErrorDescription.Should().Be("Execute 'claude login' in terminal");
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns Stale snapshot on network failures.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenNetworkFails_ReturnsStale()
    {

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ => throw new HttpRequestException("Network failure."));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Stale);
        snapshot.ErrorDescription.Should().Contain("Network failure");
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync normalizes percentage values greater than 1.0 to fractions.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenUtilizationIsPercentage_NormalizesToFraction()
    {

        const string json = """{"five_hour":{"utilization":35.0},"seven_day":{"utilization":80.0}}""";

        using var scope = new TempProfileScope(VALID_JSON);
        var discovery = new ClaudeProfileDiscovery(scope.DirectoryPath);
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);
        var provider = new ClaudeOAuthProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows[0].UsedFraction.Should().Be(0.35);
        snapshot.LimitWindows[1].UsedFraction.Should().Be(0.80);
    }

    private sealed class TempProfileScope : IDisposable
    {
        public TempProfileScope(string? jsonContent)
        {

            DirectoryPath = Path.Combine(Path.GetTempPath(), $"claude_oauth_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(DirectoryPath, ".claude"));

            if (jsonContent is not null)
                File.WriteAllText(Path.Combine(DirectoryPath, ".claude", ".credentials.json"), jsonContent);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {

            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {

            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {

            return Task.FromResult(_handler(request));
        }
    }
}
