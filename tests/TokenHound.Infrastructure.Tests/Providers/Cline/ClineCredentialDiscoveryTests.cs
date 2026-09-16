using AwesomeAssertions;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies Cline credential discovery precedence, providers.json parsing, and expiry handling.
/// </summary>
public sealed class ClineCredentialDiscoveryTests
{
    private const string SAMPLE_TOKEN = "workos:eyJhbGciOiJSUzI1NiJ9";
    private const long SAMPLE_EXPIRES_AT_MS = 1789590409000L;

    private const string SAMPLE_PROVIDERS_JSON = """
        {
          "version": 1,
          "lastUsedProvider": "cline",
          "providers": {
            "cline": {
              "settings": {
                "provider": "cline",
                "auth": {
                  "accessToken": "workos:eyJhbGciOiJSUzI1NiJ9",
                  "refreshToken": "opaque-refresh-token",
                  "expiresAt": 1789590409000,
                  "accountId": "usr-01TEST"
                }
              },
              "updatedAt": "2026-09-16T19:26:45.561Z",
              "tokenSource": "oauth"
            }
          }
        }
        """;

    /// <summary>Verifies that Parse extracts the stored token with its scheme prefix and expiry.</summary>
    [Fact]
    public void Parse_WithClineEntry_ExtractsTokenAndExpiry()
    {

        var credential = ClineProvidersJson.Parse(SAMPLE_PROVIDERS_JSON);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be(SAMPLE_TOKEN);
        credential.ExpiresAtUtc.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(SAMPLE_EXPIRES_AT_MS));
        credential.AccountId.Should().Be("usr-01TEST");
        credential.Source.Should().Be("providers.json:cline");
        credential.HasExpired(DateTimeOffset.FromUnixTimeMilliseconds(SAMPLE_EXPIRES_AT_MS - 1)).Should().BeFalse();
        credential.HasExpired(DateTimeOffset.FromUnixTimeMilliseconds(SAMPLE_EXPIRES_AT_MS)).Should().BeTrue();
    }

    /// <summary>Verifies that Parse falls back to the cline-pass entry.</summary>
    [Fact]
    public void Parse_WithClinePassFallback_ExtractsCredential()
    {

        const string json = """
            {"version":1,"providers":{"cline-pass":{"settings":{"auth":{"accessToken":"workos:pass_token"}}}}}
            """;

        var credential = ClineProvidersJson.Parse(json);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be("workos:pass_token");
        credential.Source.Should().Be("providers.json:cline-pass");
    }

    /// <summary>Verifies that environment credentials take precedence and carry the required scheme prefix.</summary>
    [Fact]
    public async Task DiscoverAsync_WithEnvironmentKey_ReturnsPrefixedToken()
    {

        var discovery = new ClineCredentialDiscovery(
            "C:\\nonexistent\\providers.json",
            static _ => "plain_token_value"
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be("workos:plain_token_value");
        credential.Source.Should().Be("environment:CLINE_API_KEY");
    }

    /// <summary>Verifies that a missing file and no environment produce no credential.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenMissing_ReturnsNull()
    {

        var discovery = new ClineCredentialDiscovery(
            "C:\\nonexistent\\providers.json",
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().BeNull();
    }

    /// <summary>Verifies that DiscoverCredentialAsync delegates to DiscoverAsync.</summary>
    [Fact]
    public async Task DiscoverCredentialAsync_Alias_DelegatesSuccessfully()
    {

        using var tempScope = new TempFileScope(SAMPLE_PROVIDERS_JSON);

        var discovery = new ClineCredentialDiscovery(
            tempScope.FilePath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverCredentialAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be(SAMPLE_TOKEN);
    }

    /// <summary>Verifies that DiscoverAsync allows non-locking shared read access while Cline rewrites the file.</summary>
    [Fact]
    public async Task DiscoverAsync_WithConcurrentWriter_ReadsWithoutLockConflict()
    {

        using var tempScope = new TempFileScope(SAMPLE_PROVIDERS_JSON);

        using var writeHandle = new FileStream(
            tempScope.FilePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete
        );

        var discovery = new ClineCredentialDiscovery(
            tempScope.FilePath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.AccessToken.Should().Be(SAMPLE_TOKEN);
    }

    /// <summary>Verifies that DiscoverAsync respects cancellation requests.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        var discovery = new ClineCredentialDiscovery();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await discovery.DiscoverAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that the default providers path targets the Cline CLI data directory.</summary>
    [Fact]
    public void ProvidersFilePath_WhenNotSpecified_TargetsDefaultLocation()
    {

        var discovery = new ClineCredentialDiscovery();

        discovery.ProvidersFilePath.Should().NotBeNullOrWhiteSpace();
        discovery.ProvidersFilePath.Should().EndWith(Path.Combine(
            ".cline",
            "data",
            "settings",
            "providers.json"
        ));
    }

    private sealed class TempFileScope : IDisposable
    {
        public TempFileScope(string content)
        {

            var dir = Path.Combine(Path.GetTempPath(), $"cline_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "providers.json");
            File.WriteAllText(FilePath, content, Encoding.UTF8);
        }

        public string FilePath { get; }

        public void Dispose()
        {

            try
            {

                var dir = Path.GetDirectoryName(FilePath);

                if (dir is not null && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception)
            {
            }
        }
    }
}