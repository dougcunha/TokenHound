using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

/// <summary>
/// Unit tests for <see cref="CursorApiClient"/>.
/// </summary>
public sealed class CursorApiClientTests
{
    private sealed class MockHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handlerFunc(request));
    }

    [Fact]
    public async Task GetUsageSummaryAsync_When200Ok_DeserializesPlanAndInjectsCookie()
    {
        // Arrange
        const string RESPONSE_JSON = """
        {
          "billingCycleStart": "2026-08-24T03:32:15.933Z",
          "billingCycleEnd": "2026-09-24T03:32:15.933Z",
          "membershipType": "pro",
          "isUnlimited": false,
          "individualUsage": {
            "plan": {
              "enabled": true,
              "used": 10,
              "limit": 500,
              "totalPercentUsed": 2.0,
              "apiPercentUsed": 0.0
            }
          }
        }
        """;

        string? capturedCookie = null;
        string? capturedUserAgent = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.Headers.Contains("Cookie"))
                capturedCookie = string.Join(";", req.Headers.GetValues("Cookie"));

            if (req.Headers.Contains("User-Agent"))
                capturedUserAgent = req.Headers.UserAgent.ToString();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new CursorApiClient(httpClient);

        // Act
        var result = await client.GetUsageSummaryAsync("user_123", "jwt_abc_token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WorkosCursorSessionToken=user_123::jwt_abc_token", capturedCookie);
        Assert.Equal("TokenHound/1.0", capturedUserAgent);
        Assert.Equal("pro", result.MembershipType);
        Assert.NotNull(result.IndividualUsage?.Plan);
        Assert.Equal(2.0, result.IndividualUsage.Plan.TotalPercentUsed);
    }

    [Fact]
    public async Task GetUsageSummaryAsync_When401Unauthorized_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        using var client = new CursorApiClient(httpClient);

        // Act
        var result = await client.GetUsageSummaryAsync("user_123", "bad_token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsageSummaryAsync_WhenArgumentsEmpty_ThrowsArgumentException()
    {
        // Arrange
        using var client = new CursorApiClient();

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await client.GetUsageSummaryAsync("", "token");
        });

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await client.GetUsageSummaryAsync("user", "");
        });
    }
}
