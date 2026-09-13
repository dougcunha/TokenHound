using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

public sealed partial class CursorUsageProviderTests
{
    [Fact]
    public async Task GetSnapshotAsync_WhenApiReturnsUnauthorized_ReturnsNeedsAuth()
    {

        var databasePath = CreateTemporaryAuthDatabase();

        try
        {
            using var httpClient = new HttpClient(new ResponseHandler(
                static _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            ));
            using var client = new CursorApiClient(httpClient);
            using var provider = new CursorUsageProvider(new CursorSessionDiscovery(databasePath), client);

            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
            Assert.Null(snapshot.ActiveBlock);
        }
        finally
        {
            DeleteTemporaryDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenApiReturnsForbidden_ReturnsNeedsAuth()
    {

        var databasePath = CreateTemporaryAuthDatabase();

        try
        {
            using var httpClient = new HttpClient(new ResponseHandler(
                static _ => new HttpResponseMessage(HttpStatusCode.Forbidden)
            ));
            using var client = new CursorApiClient(httpClient);
            using var provider = new CursorUsageProvider(new CursorSessionDiscovery(databasePath), client);

            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
            Assert.Null(snapshot.ActiveBlock);
        }
        finally
        {
            DeleteTemporaryDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenApiReturnsRateLimited_ReturnsActiveBlock()
    {

        var databasePath = CreateTemporaryAuthDatabase();
        var nowUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        try
        {
            using var httpClient = new HttpClient(new ResponseHandler(static _ => CreateRateLimitResponse()));
            using var client = new CursorApiClient(httpClient);
            using var provider = new CursorUsageProvider(
                new CursorSessionDiscovery(databasePath),
                client,
                new FixedTimeProvider(nowUtc)
            );

            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            Assert.Equal(ProviderStatus.RateLimited, snapshot.Status);
            Assert.True(snapshot.ActiveBlock?.IsBlocked);
            Assert.Equal(0, snapshot.ActiveBlock?.RetryAfterSeconds);
            Assert.True(snapshot.ActiveBlock?.ResetTimeUtc >= nowUtc.AddMinutes(1));
        }
        finally
        {
            DeleteTemporaryDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenApiReturnsRateLimitedWithoutRetryAfter_ReturnsFutureDeadline()
    {

        var databasePath = CreateTemporaryAuthDatabase();
        var nowUtc = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        try
        {
            var handler = new ResponseHandler(static _ => CreateRateLimitResponseWithoutRetryAfter());
            using var httpClient = new HttpClient(handler);
            using var client = new CursorApiClient(httpClient);
            using var provider = new CursorUsageProvider(
                new CursorSessionDiscovery(databasePath),
                client,
                new FixedTimeProvider(nowUtc)
            );

            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            Assert.Equal(ProviderStatus.RateLimited, snapshot.Status);
            Assert.True(snapshot.ActiveBlock?.IsBlocked);
            Assert.Null(snapshot.ActiveBlock?.RetryAfterSeconds);
            Assert.True(snapshot.ActiveBlock?.ResetTimeUtc >= nowUtc.AddMinutes(1));
        }
        finally
        {
            DeleteTemporaryDatabase(databasePath);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenRequestIsCancelled_PropagatesCancellation()
    {

        var databasePath = CreateTemporaryAuthDatabase();

        try
        {
            var handler = new CancellationHandler();
            using var httpClient = new HttpClient(handler);
            using var client = new CursorApiClient(httpClient);
            using var provider = new CursorUsageProvider(new CursorSessionDiscovery(databasePath), client);
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken
            );
            var snapshotTask = provider.GetSnapshotAsync(cancellation.Token).AsTask();
            await handler.RequestStarted.WaitAsync(TestContext.Current.CancellationToken);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await snapshotTask);
        }
        finally
        {
            DeleteTemporaryDatabase(databasePath);
        }
    }

    private static string CreateTemporaryAuthDatabase()
    {

        var directory = Path.Combine(Path.GetTempPath(), $"cursor_failure_{Guid.NewGuid():N}");
        var databasePath = Path.Combine(directory, "state.vscdb");
        Directory.CreateDirectory(directory);
        CreateAuthDatabase(databasePath, "test-user-id", "test-token");

        return databasePath;
    }

    private static void DeleteTemporaryDatabase(string databasePath)
    {

        var directory = Path.GetDirectoryName(databasePath);

        if (directory is not null && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    private static HttpResponseMessage CreateRateLimitResponse()
    {

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "0");

        return response;
    }

    private static HttpResponseMessage CreateRateLimitResponseWithoutRetryAfter()
        => new(HttpStatusCode.TooManyRequests);

    private sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => nowUtc;
    }

    private sealed class ResponseHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
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
