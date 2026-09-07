using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using TokenHound.Core.Contracts;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityCloudCodeClient"/>.
/// </summary>
public sealed class AntigravityCloudCodeClientTests
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
    public async Task GetAccessTokenAsync_WhenCredentialManagerHasJsonWithTokenObject_ExtractsAccessToken()
    {
        // Arrange
        const string SECRET_JSON = """
        {
          "token": {
            "access_token": "secret-access-token-123",
            "token_type": "Bearer"
          }
        }
        """;

        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(SECRET_JSON));

        using var client = new AntigravityCloudCodeClient(credentialStore: credStore);

        // Act
        var token = await client.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("secret-access-token-123", token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenCredentialManagerHasPlainString_ReturnsString()
    {
        // Arrange
        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("plain-bearer-token-456"));

        using var client = new AntigravityCloudCodeClient(credentialStore: credStore);

        // Act
        var token = await client.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("plain-bearer-token-456", token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenCredentialManagerEmpty_ReadsFromFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"oauth_creds_{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempFile, """{"access_token": "file-token-789"}""", TestContext.Current.CancellationToken);

        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        try
        {
            using var client = new AntigravityCloudCodeClient(
                credentialStore: credStore,
                credentialsFilePath: tempFile);

            // Act
            var token = await client.GetAccessTokenAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("file-token-789", token);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RetrieveUserQuotaSummaryAsync_WhenSuccessful200_DeserializesResponse()
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
                    "bucketId": "gemini-weekly",
                    "displayName": "Weekly Limit",
                    "remainingFraction": 0.85
                  }
                ]
              }
            ]
          }
        }
        """;

        string? capturedAuth = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedAuth = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_JSON, Encoding.UTF8, "application/json")
            };
        });

        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("valid-token"));

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityCloudCodeClient(
            httpClient: httpClient,
            credentialStore: credStore,
            credentialsFilePath: "nonexistent.json");

        // Act
        var result = await client.RetrieveUserQuotaSummaryAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bearer valid-token", capturedAuth);
        Assert.Single(result.Groups!);
        Assert.Equal("Gemini 2.5 Pro", result.Groups![0].DisplayName);
    }

    [Fact]
    public async Task RetrieveUserQuotaSummaryAsync_When403Forbidden_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error":{"code":403,"message":"unlicensed"}}""", Encoding.UTF8, "application/json")
        });

        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("unlicensed-token"));

        using var httpClient = new HttpClient(handler);
        using var client = new AntigravityCloudCodeClient(
            httpClient: httpClient,
            credentialStore: credStore,
            credentialsFilePath: "nonexistent.json");

        // Act
        var result = await client.RetrieveUserQuotaSummaryAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RetrieveUserQuotaSummaryAsync_WhenNoCredentials_ReturnsNull()
    {
        // Arrange
        var credStore = Substitute.For<ICredentialStore>();
        credStore.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        using var client = new AntigravityCloudCodeClient(
            credentialStore: credStore,
            credentialsFilePath: "C:\\nonexistent_file_xyz.json");

        // Act
        var result = await client.RetrieveUserQuotaSummaryAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }
}
