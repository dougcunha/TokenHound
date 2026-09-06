using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task GetSnapshotAsync_WhenLanguageServerAvailable_ReturnsOfficialSnapshot()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(1234, "--csrf_token token-abc")],
            portResolver: _ => [5555]);

        const string JSON_PAYLOAD = """
        {
          "response": {
            "groups": [
              {
                "displayName": "Gemini 2.5 Pro",
                "buckets": [
                  {
                    "bucketId": "gemini-pro-weekly",
                    "displayName": "Weekly Limit",
                    "remainingFraction": 0.80,
                    "resetTime": "2026-09-10T00:00:00Z"
                  }
                ]
              }
            ]
          }
        }
        """;

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JSON_PAYLOAD, Encoding.UTF8, "application/json")
        });

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityLanguageServerClient(httpClient);
        using var provider = new AntigravityUsageProvider(discovery, client);

        // Act
        var snapshot = await provider.GetSnapshotAsync();

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

        await File.WriteAllLinesAsync(transcriptPath,
        [
            """{"step_index": 1, "source": "MODEL", "created_at": "2026-09-06T11:00:00.000Z"}""",
            """{"step_index": 2, "source": "MODEL", "created_at": "2026-09-06T11:30:00.000Z"}"""
        ]);

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            using var provider = new AntigravityUsageProvider(discovery, null, reader);

            // Act
            var snapshot = await provider.GetSnapshotAsync();

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
    public async Task GetSnapshotAsync_WhenAllSourcesUnavailable_ReturnsNeedsAuth()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var reader = new AntigravityTranscriptReader(["C:\\nonexistent_dir_xyz_123"]);
        using var provider = new AntigravityUsageProvider(discovery, null, reader);

        // Act
        var snapshot = await provider.GetSnapshotAsync();

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("gemini", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Empty(snapshot.LimitWindows);
    }
}
