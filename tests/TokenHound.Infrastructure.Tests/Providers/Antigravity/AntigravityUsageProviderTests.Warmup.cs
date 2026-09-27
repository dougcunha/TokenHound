using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

public sealed partial class AntigravityUsageProviderTests
{
    private const string POPULATED_QUOTA_JSON =
        """{"response":{"groups":[{"displayName":"Gemini 2.5 Pro","buckets":[{"bucketId":"gemini-pro-weekly","displayName":"Weekly Limit","remainingFraction":0.80,"resetTime":"2026-09-10T00:00:00Z"}]}]}}""";

    [Fact]
    public async Task GetSnapshotAsync_WhenInitialQuotaEmptyAndWarmupSucceeds_ReturnsOfficialSnapshot()
    {

        var warmupCalled = false;
        var quotaCallCount = 0;
        using var client = CreateWarmingLanguageServerClient(
            onWarmup: () => warmupCalled = true,
            onQuota: () =>
            {
                quotaCallCount++;

                return warmupCalled ? POPULATED_QUOTA_JSON : """{"response":{"groups":[]}}""";
            });

        using var provider = new AntigravityUsageProvider(CreateRunningDiscovery(), client);
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(warmupCalled);
        Assert.Equal(2, quotaCallCount);
        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
        Assert.Single(snapshot.LimitWindows);
        Assert.Equal(0.20, snapshot.LimitWindows[0].UsedFraction!.Value, 2);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenInitialQuotaEmptyAndWarmupFails_FallsBackToDerivedTranscript()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_prov_warmup_{Guid.NewGuid():N}");
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
        await SetupTranscriptDirectoryAsync(tempDir);

        try
        {
            var snapshot = await ExecuteFailingWarmupProviderAsync(tempDir, timeProvider);

            Assert.NotNull(snapshot);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Derived, snapshot.Fidelity);
            Assert.Single(snapshot.LimitWindows);
            Assert.Equal(2, snapshot.LimitWindows[0].UsedUnits);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenInitialQuotaPopulated_DoesNotWarmupSession()
    {

        var warmupCalled = false;
        using var client = CreateWarmingLanguageServerClient(
            onWarmup: () => warmupCalled = true,
            onQuota: () => POPULATED_QUOTA_JSON);

        using var provider = new AntigravityUsageProvider(CreateRunningDiscovery(), client);
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.False(warmupCalled);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
    }

    private static AntigravityLanguageServerClient CreateWarmingLanguageServerClient(
        Action onWarmup,
        Func<string> onQuota)
    {

        var handler = new MockHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("GetUserStatus", StringComparison.OrdinalIgnoreCase))
            {
                onWarmup();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("RetrieveUserQuotaSummary", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(onQuota(), Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        return new AntigravityLanguageServerClient(new HttpClient(handler));
    }

    private static async Task SetupTranscriptDirectoryAsync(string tempDir)
    {

        var logDir = Path.Combine(tempDir, "session", ".system_generated", "logs");
        Directory.CreateDirectory(logDir);
        string[] transcriptLines =
        [
            """{"step_index": 1, "source": "MODEL", "created_at": "2026-09-06T11:00:00.000Z"}""",
            """{"step_index": 2, "source": "MODEL", "created_at": "2026-09-06T11:30:00.000Z"}"""
        ];

        await File.WriteAllLinesAsync(
            Path.Combine(logDir, "transcript.jsonl"),
            transcriptLines,
            TestContext.Current.CancellationToken);
    }

    private static async Task<Snapshot> ExecuteFailingWarmupProviderAsync(
        string tempDir,
        TimeProvider timeProvider)
    {

        using var client = CreateFailingWarmupClient();
        var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
        using var cloudClient = CreateEmptyCloudCodeClient();
        using var provider = new AntigravityUsageProvider(
            CreateRunningDiscovery(),
            client,
            reader,
            cloudClient,
            timeProvider);

        return await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
    }

    private static AntigravityLanguageServerClient CreateFailingWarmupClient()
    {

        var handler = new MockHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("GetUserStatus", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        return new AntigravityLanguageServerClient(new HttpClient(handler));
    }

    private static AntigravityCloudCodeClient CreateEmptyCloudCodeClient()
    {

        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        return new AntigravityCloudCodeClient(null, credStore, "nonexistent.json");
    }
}
