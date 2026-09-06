using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityLanguageServerClient"/>.
/// </summary>
public sealed class AntigravityLanguageServerClientTests
{
    private sealed class MockHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handlerFunc(request));
    }

    [Theory]
    [InlineData(0.85, 0.15)]
    [InlineData(1.0, 0.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(0.3, 0.7)]
    public void CalculateUsedFraction_InvertsCorrectly(double remaining, double expectedUsed)
    {
        var actual = AntigravityLanguageServerClient.CalculateUsedFraction(remaining);

        Assert.NotNull(actual);
        Assert.Equal(expectedUsed, actual.Value, 2);
    }

    [Fact]
    public void CalculateUsedFraction_WhenNull_ReturnsNull()
    {
        var actual = AntigravityLanguageServerClient.CalculateUsedFraction(null);

        Assert.Null(actual);
    }

    [Fact]
    public async Task QueryPortAsync_WhenSuccessful200_DeserializesResponse()
    {
        // Arrange
        const string RESPONSE_JSON = """
        {
          "response": {
            "groups": [
              {
                "displayName": "Gemini 2.5 Pro",
                "buckets": [
                  {
                    "bucketId": "gemini-2.5-pro-weekly",
                    "displayName": "Weekly Limit Remaining",
                    "remainingFraction": 0.85,
                    "resetTime": "2026-09-10T12:00:00Z"
                  }
                ]
              }
            ]
          }
        }
        """;

        string? capturedCsrf = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedCsrf = req.Headers.Contains("x-codeium-csrf-token")
                ? string.Join(",", req.Headers.GetValues("x-codeium-csrf-token"))
                : null;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityLanguageServerClient(httpClient);

        // Act
        var result = await client.QueryPortAsync(54321, "test-csrf-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-csrf-123", capturedCsrf);
        Assert.NotNull(result.Groups);
        Assert.Single(result.Groups);
        Assert.Equal("Gemini 2.5 Pro", result.Groups[0].DisplayName);
        Assert.Single(result.Groups[0].Buckets!);
        Assert.Equal(0.85, result.Groups[0].Buckets![0].RemainingFraction);
    }

    [Fact]
    public async Task QueryPortAsync_When403Forbidden_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityLanguageServerClient(httpClient);

        // Act
        var result = await client.QueryPortAsync(54321, "wrong-csrf");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RetrieveUserQuotaSummaryAsync_TriesMultiplePortsUntilSuccess()
    {
        // Arrange
        var endpoint = new AntigravityEndpoint
        {
            ProcessId = 1234,
            CsrfToken = "token-xyz",
            CandidatePorts = [5001, 5002]
        };

        var handler = new MockHttpMessageHandler(req =>
        {

            if (req.RequestUri!.Port == 5001)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response": {"groups": []}}""", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityLanguageServerClient(httpClient);

        // Act
        var result = await client.RetrieveUserQuotaSummaryAsync(endpoint);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Groups);
    }
}
