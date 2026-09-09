using AwesomeAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies HTTP request formation, header injection, JSON deserialization, and error translation in ClaudeOAuthClient.
/// </summary>
public sealed class ClaudeOAuthClientTests
{
    private const string USAGE_JSON_PAYLOAD = """
        {
          "five_hour": {
            "utilization": 35.0,
            "resets_at": "2026-08-28T18:00:00Z"
          },
          "seven_day": {
            "utilization": 72.0,
            "resets_at": "2026-09-01T00:00:00Z"
          }
        }
        """;

    /// <summary>
    /// Verifies that GetUsageAsync parses five_hour and seven_day limit windows from valid HTTP 200 JSON.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenSuccessful200_DeserializesFiveHourAndSevenDay()
    {

        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(USAGE_JSON_PAYLOAD, Encoding.UTF8, "application/json")
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var response = await client.GetUsageAsync("test-token-123", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response!.FiveHour.Should().NotBeNull();
        response.FiveHour!.Utilization.Should().Be(35.0);
        response.FiveHour.ResetsAt.Should().Be(DateTimeOffset.Parse("2026-08-28T18:00:00Z"));

        response.SevenDay.Should().NotBeNull();
        response.SevenDay!.Utilization.Should().Be(72.0);
        response.SevenDay.ResetsAt.Should().Be(DateTimeOffset.Parse("2026-09-01T00:00:00Z"));
    }

    /// <summary>
    /// Verifies that GetUsageAsync injects required Authorization, anthropic-beta, and User-Agent headers.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_SendsRequiredHeadersAndEndpoint()
    {

        HttpRequestMessage? capturedRequest = null;

        var handler = new MockHttpMessageHandler(req =>
        {

            capturedRequest = req;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        await client.GetUsageAsync("secret-bearer-token", TestContext.Current.CancellationToken);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().Be(new Uri(ClaudeOAuthClient.DEFAULT_USAGE_ENDPOINT));
        capturedRequest.Method.Should().Be(HttpMethod.Get);
        capturedRequest.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("secret-bearer-token");
        capturedRequest.Headers.GetValues("anthropic-beta").Should().ContainSingle().Which.Should().Be("oauth-2025-04-20");
        capturedRequest.Headers.UserAgent.ToString().Should().Be("TokenHound/1.0");
    }

    /// <summary>
    /// Verifies that GetUsageAsync throws HttpRequestException with Unauthorized status code on HTTP 401.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenUnauthorized401_ThrowsHttpRequestExceptionWithUnauthorized()
    {

        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync("bad-token", TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Verifies that GetUsageAsync throws HttpRequestException with Forbidden status code on HTTP 403.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenForbidden403_ThrowsHttpRequestExceptionWithForbidden()
    {

        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync("forbidden-token", TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Verifies that GetUsageAsync throws RateLimitException and extracts Retry-After seconds on HTTP 429.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenRateLimited429WithSecondsHeader_ThrowsRateLimitException()
    {

        var handler = new MockHttpMessageHandler(static _ =>
        {

            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "120");

            return response;
        });

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync("token-429", TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<ClaudeOAuthClient.RateLimitException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.Which.RetryAfterSeconds.Should().Be(120);
        ex.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>
    /// Verifies that GetUsageAsync throws RateLimitException with null RetryAfter when header is omitted.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenRateLimited429WithoutRetryAfter_ThrowsRateLimitExceptionWithNullRetryAfter()
    {

        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync("token-429-no-header", TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<ClaudeOAuthClient.RateLimitException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.Which.RetryAfter.Should().BeNull();
        ex.Which.RetryAfterSeconds.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetUsageAsync throws HttpRequestException with InternalServerError on HTTP 500.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenServerError500_ThrowsHttpRequestExceptionWithInternalServerError()
    {

        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var httpClient = new HttpClient(handler);
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync("token", TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Verifies that GetUsageAsync rejects null or whitespace access tokens with ArgumentException.
    /// </summary>
    /// <param name="invalidToken">The invalid token input.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUsageAsync_WhenAccessTokenNullOrWhitespace_ThrowsArgumentException(string? invalidToken)
    {

        using var httpClient = new HttpClient();
        var client = new ClaudeOAuthClient(httpClient);

        var act = async () => await client.GetUsageAsync(invalidToken!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that GetUsageAsync honors and propagates cancellation tokens.
    /// </summary>
    [Fact]
    public async Task GetUsageAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        using var httpClient = new HttpClient();
        var client = new ClaudeOAuthClient(httpClient);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await client.GetUsageAsync("token", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
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
