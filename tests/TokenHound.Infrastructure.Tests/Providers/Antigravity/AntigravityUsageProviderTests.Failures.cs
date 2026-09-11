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
    [Fact]
    public async Task GetSnapshotAsync_WhenCloudCodeUnauthorizedAndServerRunning_ReturnsNeedsAuth()
    {

        using var languageHttpClient = new HttpClient(new MockHttpMessageHandler(
            static _ => CreateEmptyGroupsResponse()
        ));
        using var languageClient = new AntigravityLanguageServerClient(languageHttpClient);
        using var httpClient = new HttpClient(new MockHttpMessageHandler(
            static _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        ));
        using var cloudClient = new AntigravityCloudCodeClient(
            httpClient,
            CreateCredentialStore(),
            "nonexistent.json"
        );
        using var provider = new AntigravityUsageProvider(
            CreateRunningDiscovery(),
            languageClient,
            new AntigravityTranscriptReader([]),
            cloudClient
        );

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Null(snapshot.ActiveBlock);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCloudCodeRateLimited_AttachesBlockToTranscriptFallback()
    {

        var nowUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var transcriptDirectory = await CreateTranscriptDirectoryAsync(nowUtc);

        try
        {
            var snapshot = await GetRateLimitedTranscriptSnapshotAsync(nowUtc, transcriptDirectory);

            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(Fidelity.Derived, snapshot.Fidelity);
            Assert.Equal(0, snapshot.ActiveBlock?.RetryAfterSeconds);
            Assert.True(snapshot.ActiveBlock?.ResetTimeUtc >= nowUtc.AddMinutes(1));
        }
        finally
        {
            Directory.Delete(transcriptDirectory, true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCloudCodeRequestIsCancelled_PropagatesCancellation()
    {

        var handler = new CancellationHandler();
        using var httpClient = new HttpClient(handler);
        using var cloudClient = new AntigravityCloudCodeClient(
            httpClient,
            CreateCredentialStore(),
            "nonexistent.json"
        );
        using var provider = new AntigravityUsageProvider(
            CreateUnavailableDiscovery(),
            null,
            new AntigravityTranscriptReader([]),
            cloudClient
        );
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        var snapshotTask = provider.GetSnapshotAsync(cancellation.Token).AsTask();
        await handler.RequestStarted.WaitAsync(TestContext.Current.CancellationToken);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await snapshotTask);
    }

    private static AntigravityEndpointDiscovery CreateUnavailableDiscovery()
        => new(
            processEnumerator: static () => [],
            portResolver: static _ => []
        );

    private static AntigravityEndpointDiscovery CreateRunningDiscovery()
        => new(
            processEnumerator: static () => [(1234, "--csrf_token token-abc")],
            portResolver: static _ => [5555]
        );

    private static HttpResponseMessage CreateEmptyGroupsResponse()
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"response":{"groups":[]}}""",
                Encoding.UTF8,
                "application/json"
            )
        };

    private static ICredentialStore CreateCredentialStore()
    {

        var credentialStore = Substitute.For<ICredentialStore>();
        credentialStore.ReadCredentialAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("test-oauth-token"));

        return credentialStore;
    }

    private static async Task<Snapshot> GetRateLimitedTranscriptSnapshotAsync(
        DateTimeOffset nowUtc,
        string transcriptDirectory)
    {

        using var httpClient = new HttpClient(new MockHttpMessageHandler(
            static _ => CreateRateLimitResponse()
        ));
        using var cloudClient = new AntigravityCloudCodeClient(
            httpClient,
            CreateCredentialStore(),
            "nonexistent.json"
        );
        var timeProvider = new FakeTimeProvider(nowUtc);
        var reader = new AntigravityTranscriptReader([transcriptDirectory], timeProvider);
        using var provider = new AntigravityUsageProvider(
            CreateUnavailableDiscovery(),
            null,
            reader,
            cloudClient,
            timeProvider
        );

        return await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string> CreateTranscriptDirectoryAsync(DateTimeOffset nowUtc)
    {

        var directory = Path.Combine(Path.GetTempPath(), $"gemini_rate_limit_{Guid.NewGuid():N}");
        var logDirectory = Path.Combine(directory, "conversation", ".system_generated", "logs");
        Directory.CreateDirectory(logDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(logDirectory, "transcript.jsonl"),
            $"{{\"step_index\":1,\"source\":\"MODEL\",\"created_at\":\"{nowUtc:O}\"}}",
            TestContext.Current.CancellationToken
        );

        return directory;
    }

    private static HttpResponseMessage CreateRateLimitResponse()
    {

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "0");

        return response;
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _requestStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        internal Task RequestStarted
            => _requestStarted.Task;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {

            _requestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
