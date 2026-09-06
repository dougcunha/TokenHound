using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies Claude Code credential discovery, profile enumeration, JSON extraction, and expired token detection.
/// </summary>
public sealed class ClaudeProfileDiscoveryTests
{
    private const string SAMPLE_OAUTH_JSON = """
        {
          "claudeAiOauth": {
            "accessToken": "sk-ant-oat01-test-token-12345",
            "expiresAt": 1757121000000
          }
        }
        """;

    /// <summary>
    /// Verifies that ParseCredentialJson extracts token and millisecond epoch expiry from the claudeAiOauth section.
    /// </summary>
    [Fact]
    public void ParseCredentialJson_WithClaudeAiOauthSection_ExtractsTokenAndExpiry()
    {

        var credential = ClaudeProfileDiscovery.ParseCredentialJson(SAMPLE_OAUTH_JSON);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be("sk-ant-oat01-test-token-12345");
        credential.ExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1757121000000));
    }

    /// <summary>
    /// Verifies that ParseCredentialJson extracts flat token and ISO 8601 string expiry.
    /// </summary>
    [Fact]
    public void ParseCredentialJson_WithFlatJsonAndIsoDate_ExtractsTokenAndExpiry()
    {

        const string json = """
            {
              "token": "sk-ant-flat-token-99999",
              "expiresAt": "2026-09-06T15:00:00.000Z"
            }
            """;

        var credential = ClaudeProfileDiscovery.ParseCredentialJson(json);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be("sk-ant-flat-token-99999");
        credential.ExpiresAt.Should().Be(DateTimeOffset.Parse("2026-09-06T15:00:00.000Z"));
    }

    /// <summary>
    /// Verifies that ParseCredentialJson returns null when given invalid, empty, or malformed JSON.
    /// </summary>
    /// <param name="invalidJson">The invalid JSON string payload.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ invalid json syntax ")]
    [InlineData("[]")]
    [InlineData("{\"otherField\": 123}")]
    public void ParseCredentialJson_WithInvalidOrIncompleteJson_ReturnsNull(string? invalidJson)
    {

        var credential = ClaudeProfileDiscovery.ParseCredentialJson(invalidJson);

        credential.Should().BeNull();
    }

    /// <summary>
    /// Verifies expired token detection logic for past, future, and missing expiry dates.
    /// </summary>
    [Fact]
    public void IsExpired_WithVariousExpiryDates_DetectsExpirationAccurately()
    {

        var pastToken = new ClaudeCredentialDto
        {
            AccessToken = "token-past",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        };

        var futureToken = new ClaudeCredentialDto
        {
            AccessToken = "token-future",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
        };

        var noExpiryToken = new ClaudeCredentialDto { AccessToken = "token-no-expiry" };

        pastToken.IsExpired.Should().BeTrue();
        futureToken.IsExpired.Should().BeFalse();
        noExpiryToken.IsExpired.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that DiscoverDefaultCredentialAsync loads and parses .credentials.json in the .claude directory.
    /// </summary>
    [Fact]
    public async Task DiscoverDefaultCredentialAsync_WhenFileExists_LoadsCredentialSuccessfully()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_test_{Guid.NewGuid():N}");
        var claudeDir = Path.Combine(tempDir, ".claude");
        Directory.CreateDirectory(claudeDir);

        try
        {

            var credPath = Path.Combine(claudeDir, ".credentials.json");
            await File.WriteAllTextAsync(credPath, SAMPLE_OAUTH_JSON, TestContext.Current.CancellationToken);

            var discovery = new ClaudeProfileDiscovery(tempDir);
            var credential = await discovery.DiscoverDefaultCredentialAsync(TestContext.Current.CancellationToken);

            credential.Should().NotBeNull();
            credential!.AccessToken.Should().Be("sk-ant-oat01-test-token-12345");
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that DiscoverDefaultCredentialAsync returns null when the directory or credentials file is missing.
    /// </summary>
    [Fact]
    public async Task DiscoverDefaultCredentialAsync_WhenFileMissing_ReturnsNullGracefully()
    {

        var missingDir = Path.Combine(Path.GetTempPath(), $"missing_claude_{Guid.NewGuid():N}");
        var discovery = new ClaudeProfileDiscovery(missingDir);

        var credential = await discovery.DiscoverDefaultCredentialAsync(TestContext.Current.CancellationToken);

        credential.Should().BeNull();
    }

    /// <summary>
    /// Verifies that DiscoverCredentialAsync falls back to multi-profile directories when default credentials are missing.
    /// </summary>
    [Fact]
    public async Task DiscoverCredentialAsync_WhenDefaultMissing_DiscoversMultiProfileCredential()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_multi_{Guid.NewGuid():N}");
        var profileDir = Path.Combine(tempDir, ".claude-work");
        Directory.CreateDirectory(profileDir);

        try
        {

            var credPath = Path.Combine(profileDir, ".credentials.json");
            await File.WriteAllTextAsync(credPath, SAMPLE_OAUTH_JSON, TestContext.Current.CancellationToken);

            var discovery = new ClaudeProfileDiscovery(tempDir);
            var credential = await discovery.DiscoverCredentialAsync(TestContext.Current.CancellationToken);

            credential.Should().NotBeNull();
            credential!.AccessToken.Should().Be("sk-ant-oat01-test-token-12345");
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that DiscoverAllCredentialsAsync discovers all credentials across default and multiple profile directories.
    /// </summary>
    [Fact]
    public async Task DiscoverAllCredentialsAsync_WithMultipleProfiles_ReturnsAllDiscoveredCredentials()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_all_{Guid.NewGuid():N}");
        var defaultDir = Path.Combine(tempDir, ".claude");
        var workDir = Path.Combine(tempDir, ".claude-work");
        Directory.CreateDirectory(defaultDir);
        Directory.CreateDirectory(workDir);

        try
        {

            await File.WriteAllTextAsync(
                Path.Combine(defaultDir, ".credentials.json"),
                SAMPLE_OAUTH_JSON,
                TestContext.Current.CancellationToken
            );
            await File.WriteAllTextAsync(
                Path.Combine(workDir, ".credentials.json"),
                SAMPLE_OAUTH_JSON,
                TestContext.Current.CancellationToken
            );

            var discovery = new ClaudeProfileDiscovery(tempDir);
            var all = await discovery.DiscoverAllCredentialsAsync(TestContext.Current.CancellationToken);

            all.Should().HaveCount(2);
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that DiscoverCredentialAsync respects and propagates cancellation requests.
    /// </summary>
    [Fact]
    public async Task DiscoverCredentialAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        var discovery = new ClaudeProfileDiscovery();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await discovery.DiscoverCredentialAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
