using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityUsageProvider"/>.
/// </summary>
public sealed class AntigravityUsageProviderTests
{
    private sealed class MockHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handlerFunc(request));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public void ProviderId_IsGemini()
    {
        using var provider = new AntigravityUsageProvider();

        Assert.Equal("gemini", provider.ProviderId);
    }

    [Fact]
    public async Task GetSnapshotAsync_LiveIntegration_WhenAgyRunning_ReturnsOfficialMetrics()
    {
        using var provider = new AntigravityUsageProvider();
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.Equal("gemini", snapshot.ProviderId);

        if (OperatingSystem.IsWindows() && Process.GetProcessesByName("agy").Length > 0)
        {
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Official, snapshot.Fidelity);
            Assert.NotEmpty(snapshot.LimitWindows);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenLanguageServerAvailable_ReturnsOfficialSnapshot()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(1234, "--csrf_token token-abc")],
            portResolver: _ => [5555]);

        const string JSON_PAYLOAD = """{"response":{"groups":[{"displayName":"Gemini 2.5 Pro","buckets":[{"bucketId":"gemini-pro-weekly","displayName":"Weekly Limit","remainingFraction":0.80,"resetTime":"2026-09-10T00:00:00Z"}]}]}}""";

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JSON_PAYLOAD, Encoding.UTF8, "application/json")
        });

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityLanguageServerClient(httpClient);
        using var provider = new AntigravityUsageProvider(discovery, client);

        // Act
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("gemini", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
        Assert.Single(snapshot.LimitWindows);

        var window = snapshot.LimitWindows[0];
        Assert.Equal("Weekly Limit", window.Name);
        Assert.Equal(0.20, window.UsedFraction!.Value, 2); // 1.0 - 0.80
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenLanguageServerUnavailable_FallsBackToTranscripts()
    {
        // Arrange: discovery returns null
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_prov_{Guid.NewGuid():N}");
        var logDir = Path.Combine(tempDir, "session", ".system_generated", "logs");
        Directory.CreateDirectory(logDir);

        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
        var transcriptPath = Path.Combine(logDir, "transcript.jsonl");
        string[] transcriptLines =
        [
            """{"step_index": 1, "source": "MODEL", "created_at": "2026-09-06T11:00:00.000Z"}""",
            """{"step_index": 2, "source": "MODEL", "created_at": "2026-09-06T11:30:00.000Z"}"""
        ];

        await File.WriteAllLinesAsync(transcriptPath, transcriptLines, TestContext.Current.CancellationToken);

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            using var provider = new AntigravityUsageProvider(discovery, null, reader);

            // Act
            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("gemini", snapshot.ProviderId);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Derived, snapshot.Fidelity);
            Assert.Single(snapshot.LimitWindows);

            var window = snapshot.LimitWindows[0];
            Assert.Equal("Requests Today", window.Name);
            Assert.Equal(2, window.RemainingUnits);
            Assert.Null(window.TotalUnits); // Zero Fake Data
            Assert.Null(window.UsedFraction); // Zero Fake Data
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCloudCodeAvailable_ReturnsOfficialSnapshot()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        const string JSON_PAYLOAD = """{"response":{"groups":[{"displayName":"Gemini 2.5 Pro","buckets":[{"bucketId":"gemini-pro-cloud","displayName":"Weekly Quota","remainingFraction":0.75,"resetTime":"2026-09-12T00:00:00Z"}]}]}}""";

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JSON_PAYLOAD, Encoding.UTF8, "application/json")
        });

        var credStore = Substitute.For<TokenHound.Core.Contracts.ICredentialStore>();
        credStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("test-oauth-token"));

        using var httpClient = new HttpClient(handler);
        using var cloudClient = new AntigravityCloudCodeClient(httpClient, credStore, "nonexistent_file.json");
        using var provider = new AntigravityUsageProvider(discovery, null, null, cloudClient);

        // Act
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("gemini", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
        Assert.Single(snapshot.LimitWindows);
        Assert.Equal(0.25, snapshot.LimitWindows[0].UsedFraction!.Value, 2);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCloudCode403_FallsBackToTranscripts()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error": {"code": 403, "message": "unlicensed #3501"}}""", Encoding.UTF8, "application/json")
        });

        var credStore = Substitute.For<TokenHound.Core.Contracts.ICredentialStore>();
        credStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("test-oauth-token"));

        using var httpClient = new HttpClient(handler);
        using var cloudClient = new AntigravityCloudCodeClient(httpClient, credStore, "nonexistent_file.json");

        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_prov_403_{Guid.NewGuid():N}");
        var logDir = Path.Combine(tempDir, "conv", ".system_generated", "logs");
        Directory.CreateDirectory(logDir);

        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
        var transcriptPath = Path.Combine(logDir, "transcript.jsonl");
        string[] logs403 = ["""{"step_index": 1, "source": "MODEL", "created_at": "2026-09-06T11:00:00.000Z"}"""];

        await File.WriteAllLinesAsync(transcriptPath, logs403, TestContext.Current.CancellationToken);

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            using var provider = new AntigravityUsageProvider(discovery, null, reader, cloudClient);

            // Act
            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("gemini", snapshot.ProviderId);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Derived, snapshot.Fidelity);
            Assert.Single(snapshot.LimitWindows);
            Assert.Equal(1, snapshot.LimitWindows[0].RemainingUnits);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenZeroRequestsToday_ButTranscriptsExist_ReturnsOkWithZeroUnits()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_prov_zero_{Guid.NewGuid():N}");
        var logDir = Path.Combine(tempDir, "conv", ".system_generated", "logs");
        Directory.CreateDirectory(logDir);

        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
        var transcriptPath = Path.Combine(logDir, "transcript.jsonl");

        // Old request from yesterday
        string[] logsZero = ["""{"step_index": 1, "source": "MODEL", "created_at": "2026-09-05T11:00:00.000Z"}"""];

        await File.WriteAllLinesAsync(transcriptPath, logsZero, TestContext.Current.CancellationToken);

        var credStore = Substitute.For<TokenHound.Core.Contracts.ICredentialStore>();
        credStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        using var cloudClient = new AntigravityCloudCodeClient(null, credStore, "nonexistent.json");

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            using var provider = new AntigravityUsageProvider(discovery, null, reader, cloudClient);

            // Act
            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("gemini", snapshot.ProviderId);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Derived, snapshot.Fidelity);
            Assert.Single(snapshot.LimitWindows);
            Assert.Equal(0, snapshot.LimitWindows[0].RemainingUnits);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenAllSourcesUnavailable_ReturnsNeedsAuth()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var credStore = Substitute.For<TokenHound.Core.Contracts.ICredentialStore>();
        credStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        using var cloudClient = new AntigravityCloudCodeClient(null, credStore, "C:\\nonexistent_oauth.json");
        var reader = new AntigravityTranscriptReader(["C:\\nonexistent_dir_xyz_123"]);
        using var provider = new AntigravityUsageProvider(discovery, null, reader, cloudClient);

        // Act
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("gemini", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Empty(snapshot.LimitWindows);
    }
}
