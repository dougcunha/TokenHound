using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

public sealed partial class CopilotBillingClientTests
{
    [Fact]
    public async Task GetBillingUsageAsync_When429_ThrowsCopilotApiExceptionWithRetryAfter()
    {
        var handler = new TestHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "120");
            return response;
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        var ex = await Assert.ThrowsAsync<CopilotApiException>(() => client.GetBillingUsageAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        ));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(120, ex.RetryAfterSeconds);
    }

    [Fact]
    public async Task GetBillingUsageAsync_When403_ThrowsCopilotApiException()
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        var ex = await Assert.ThrowsAsync<CopilotApiException>(() => client.GetBillingUsageAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        ));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task GetBillingUsageAsync_WhenTimeout_ThrowsCopilotTimeoutException()
    {
        var handler = new TestHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(
            http,
            timeout: TimeSpan.FromMilliseconds(20)
        );

        await Assert.ThrowsAsync<CopilotTimeoutException>(() => client.GetBillingUsageAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task GetBillingUsageAsync_WhenCancelled_PropagatesOperationCanceledException()
    {
        var handler = new TestHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetBillingUsageAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            2026,
            9,
            "fixture-token",
            cts.Token
        ));
    }
}
