using AwesomeAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.OpenCode;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.OpenCode;

/// <summary>
/// Unit tests verifying <see cref="OpenCodeApiClient"/> HTTP handling, headers, timeouts, and error parsing.
/// </summary>
public sealed class OpenCodeApiClientTests
{
    private const string SUCCESS_JSON = """{"usage":{"rolling":{"status":"ok","percent":12.5,"resetsAt":"2026-09-12T00:45:07.613Z"},"weekly":{"status":"ok","percent":34.0,"resetsAt":"2026-09-14T00:00:00.613Z"},"monthly":{"status":"ok","percent":58.2,"resetsAt":"2026-10-10T14:55:16.613Z"}}}""";
    private const string RATE_LIMIT_JSON = """{"type":"error","error":{"type":"GoUsageLimitError","message":"OpenCode Go 5 hour usage limit reached."},"metadata":{"workspace":"wrk_1","limitName":"5 hour"}}""";
    private const string AUTH_ERROR_JSON = """{"type":"error","error":{"type":"AuthError","message":"Invalid API key provided."}}""";
    private const string ENTITLEMENT_JSON = """{"type":"error","error":{"type":"EntitlementError","message":"OpenCode Go subscription required."}}""";

    /// <summary>Verifies successful deserialization and request headers for HTTP 200 OK.</summary>
    [Fact]
    public async Task GetUsageAsync_When200Ok_DeserializesWindowsAndSetsHeadersAsync()
    {

        string? capturedAuth = null;
        string? capturedUserAgent = null;
        string? capturedAccept = null;

        var handler = new MockHttpMessageHandler(req =>
        {

            capturedAuth = req.Headers.Authorization?.ToString();
            capturedUserAgent = req.Headers.GetValues("User-Agent").FirstOrDefault();
            capturedAccept = req.Headers.Accept.ToString();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var client = new OpenCodeApiClient(handler);
        var result = await client.GetUsageAsync("zen_live_test_key", TestContext.Current.CancellationToken);

        capturedAuth.Should().Be("Bearer zen_live_test_key");
        capturedUserAgent.Should().Be("TokenHound");
        capturedAccept.Should().Be("application/json");
        result.Usage.Rolling.Percent.Should().Be(12.5);
        result.Usage.Weekly.Percent.Should().Be(34.0);
        result.Usage.Monthly.Percent.Should().Be(58.2);
    }

    /// <summary>Verifies that incomplete 200 responses throw JsonException.</summary>
    [Fact]
    public async Task GetUsageAsync_When200OkMissingWindows_ThrowsJsonExceptionAsync()
    {

        const string incompleteJson = """{"usage":{"rolling":{"status":"ok","percent":10.0,"resetsAt":"2026-09-12T00:00:00Z"}}}""";
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(incompleteJson, Encoding.UTF8, "application/json")
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("test-key", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    /// <summary>Verifies that HTTP 401 throws OpenCodeAuthException with error response details.</summary>
    [Fact]
    public async Task GetUsageAsync_When401Unauthorized_ThrowsOpenCodeAuthExceptionAsync()
    {

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(AUTH_ERROR_JSON, Encoding.UTF8, "application/json")
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("bad-key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeAuthException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.ErrorResponse?.Error?.Type.Should().Be("AuthError");
        ex.Message.Should().Contain("Invalid API key");
    }

    /// <summary>Verifies that HTTP 403 throws OpenCodeEntitlementException with entitlement message.</summary>
    [Fact]
    public async Task GetUsageAsync_When403Forbidden_ThrowsOpenCodeEntitlementExceptionAsync()
    {

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(ENTITLEMENT_JSON, Encoding.UTF8, "application/json")
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("no-go-key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeEntitlementException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.ErrorResponse?.Error?.Type.Should().Be("EntitlementError");
        ex.Message.Should().Contain("subscription required");
    }

    /// <summary>Verifies that HTTP 429 with integer Retry-After parses seconds and metadata.</summary>
    [Fact]
    public async Task GetUsageAsync_When429WithSecondsHeader_ThrowsOpenCodeRateLimitExceptionAsync()
    {

        var handler = new MockHttpMessageHandler(_ =>
        {

            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(RATE_LIMIT_JSON, Encoding.UTF8, "application/json")
            };
            resp.Headers.TryAddWithoutValidation("Retry-After", "120");

            return resp;
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeRateLimitException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.RetryAfterSeconds.Should().Be(120);
        ex.ErrorResponse?.Metadata?.LimitName.Should().Be("5 hour");
    }

    /// <summary>Verifies that HTTP 429 with HTTP date parses Retry-After seconds via TimeProvider.</summary>
    [Fact]
    public async Task GetUsageAsync_When429WithHttpDateHeader_ExtractsRetryAfterSecondsAsync()
    {

        var fakeNow = new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fakeNow);

        var handler = new MockHttpMessageHandler(_ =>
        {

            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.TryAddWithoutValidation("Retry-After", "Fri, 11 Sep 2026 12:05:00 GMT");

            return resp;
        });

        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeRateLimitException>()).Which;
        ex.RetryAfterSeconds.Should().Be(300);
    }

    /// <summary>Verifies that HTTP 429 without Retry-After header sets null seconds.</summary>
    [Fact]
    public async Task GetUsageAsync_When429WithoutRetryAfter_ThrowsRateLimitExceptionWithNullRetryAfterAsync()
    {

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeRateLimitException>()).Which;
        ex.RetryAfterSeconds.Should().BeNull();
    }

    /// <summary>Verifies that HTTP 429 carries the body reset timestamp through the exception boundary.</summary>
    [Fact]
    public async Task GetUsageAsync_When429HasResetTimestamp_PreservesResetTimestampAsync()
    {

        var resetTimeUtc = new DateTimeOffset(2026, 9, 12, 12, 30, 0, TimeSpan.Zero);
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    type = "error",
                    resetsAt = resetTimeUtc,
                    error = new { type = "GoUsageLimitError" }
                }),
                Encoding.UTF8,
                "application/json"
            )
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeRateLimitException>()).Which;
        ex.ErrorResponse?.ResetsAt.Should().Be(resetTimeUtc);
        ex.ResetTimeUtc.Should().Be(resetTimeUtc);
    }

    /// <summary>Verifies that an invalid HTTP 429 reset timestamp is ignored safely.</summary>
    [Fact]
    public async Task GetUsageAsync_When429HasInvalidResetTimestamp_DoesNotExposeResetAsync()
    {

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(
                """{"type":"error","resetsAt":"not-a-timestamp","error":{"type":"GoUsageLimitError"}}""",
                Encoding.UTF8,
                "application/json"
            )
        });

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        var ex = (await act.Should().ThrowAsync<OpenCodeRateLimitException>()).Which;
        ex.ResetTimeUtc.Should().BeNull();
    }

    /// <summary>Verifies that request timeout throws OpenCodeTimeoutException.</summary>
    [Fact]
    public async Task GetUsageAsync_WhenTimeoutOccurs_ThrowsOpenCodeTimeoutExceptionAsync()
    {

        var handler = new MockHttpMessageHandler(async (_, ct) =>
        {

            await Task.Delay(Timeout.InfiniteTimeSpan, ct);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = new OpenCodeApiClient(handler, timeout: TimeSpan.FromMilliseconds(30));
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OpenCodeTimeoutException>();
    }

    /// <summary>Verifies that external cancellation propagates OperationCanceledException.</summary>
    [Fact]
    public async Task GetUsageAsync_WhenCallerCancels_PropagatesOperationCanceledExceptionAsync()
    {

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that transient socket failures propagate HttpRequestException.</summary>
    [Fact]
    public async Task GetUsageAsync_WhenSocketTimeout_PropagatesHttpRequestExceptionAsync()
    {

        var handler = new MockHttpMessageHandler(static _ =>
            throw new HttpRequestException("Socket failure", new SocketException((int)SocketError.TimedOut)));

        using var client = new OpenCodeApiClient(handler);
        var act = async () => await client.GetUsageAsync("key", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>Verifies constructor rejects timeouts greater than 10 seconds or non-positive.</summary>
    [Fact]
    public void Constructor_WhenTimeoutInvalid_ThrowsArgumentOutOfRangeException()
    {

        var actTooHigh = () => new OpenCodeApiClient(timeout: TimeSpan.FromSeconds(15));
        var actZero = () => new OpenCodeApiClient(timeout: TimeSpan.Zero);

        actTooHigh.Should().Throw<ArgumentOutOfRangeException>();
        actZero.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies that blank API key values throw ArgumentException.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUsageAsync_WhenApiKeyBlank_ThrowsArgumentExceptionAsync(string key)
    {

        using var client = new OpenCodeApiClient();
        var act = async () => await client.GetUsageAsync(key, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class MockHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc) : HttpMessageHandler
    {
        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> syncHandler)
            : this((req, _) => Task.FromResult(syncHandler(req)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handlerFunc(request, cancellationToken);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
}
