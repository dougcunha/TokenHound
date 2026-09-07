using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>Verifies the exact bounded Copilot HTTP request and typed failures.</summary>
public sealed class CopilotApiClientTests
{
    [Fact]
    public async Task GetQuotaAsync_SendsExactGetHeadersAndDeserializesResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"quota_reset_date_utc\":\"2026-10-01T00:00:00Z\",\"quota_snapshots\":{}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        using var client = new CopilotApiClient(
            httpClient,
            new Uri(CopilotApiClient.DEFAULT_ENDPOINT),
            CopilotApiClient.DEFAULT_TIMEOUT);

        var result = await client.GetQuotaAsync(
            "gho-fixture-token",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(result.QuotaSnapshots);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal(new Uri(CopilotApiClient.DEFAULT_ENDPOINT), capturedRequest.RequestUri);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization!.Scheme);
        Assert.Equal("gho-fixture-token", capturedRequest.Headers.Authorization.Parameter);
        Assert.Equal(CopilotApiClient.USER_AGENT_VALUE, capturedRequest.Headers.UserAgent.ToString());
        Assert.Equal("application/json", capturedRequest.Headers.Accept.ToString());
        Assert.Equal(CopilotApiClient.DEFAULT_TIMEOUT, client.Timeout);
    }

    [Fact]
    public async Task GetQuotaAsync_MapsHttpStatusAndRetryAfterWithoutReadingBody()
    {
        var handler = new TestHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "0");
            return response;
        });
        using var httpClient = new HttpClient(handler);
        using var client = new CopilotApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<CopilotApiException>(
            () => client.GetQuotaAsync(
                "gho-fixture-token",
                TestContext.Current.CancellationToken
            ));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(0, exception.RetryAfterSeconds);
        Assert.DoesNotContain("gho-fixture-token", exception.Message);
    }

    [Fact]
    public async Task GetQuotaAsync_ConvertsProviderTimeoutToTypedTimeout()
    {
        var handler = new TestHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        using var client = new CopilotApiClient(
            httpClient,
            new Uri(CopilotApiClient.DEFAULT_ENDPOINT),
            TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAsync<CopilotTimeoutException>(
            () => client.GetQuotaAsync(
                "gho-fixture-token",
                TestContext.Current.CancellationToken
            ));
    }

    [Fact]
    public async Task GetQuotaAsync_PropagatesCallerCancellation()
    {
        var handler = new TestHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        using var client = new CopilotApiClient(httpClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetQuotaAsync("gho-fixture-token", cancellation.Token));
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _asyncHandler;

        public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public TestHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _asyncHandler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_asyncHandler is not null)
                return _asyncHandler(request, cancellationToken);

            return Task.FromResult(_handler!(request));
        }
    }
}
