using AwesomeAssertions;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.OpenCode;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.OpenCode;

/// <summary>
/// Verifies OpenCode credential discovery precedence, JSON parsing, file sharing, and cancellation behavior.
/// </summary>
public sealed class OpenCodeCredentialDiscoveryTests
{
    private const string SAMPLE_OPENCODE_GO_JSON = """{"opencode-go":{"type":"api","key":"zen_live_go_12345"}}""";
    private const string SAMPLE_OPENCODE_FALLBACK_JSON = """{"opencode":{"type":"api","key":"zen_live_fallback_67890"}}""";
    private const string SAMPLE_BOTH_KEYS_JSON = """{"opencode-go":{"type":"api","key":"zen_live_primary"},"opencode":{"type":"api","key":"zen_live_secondary"}}""";

    /// <summary>Verifies that ParseAuthJson extracts the opencode-go key and source.</summary>
    [Fact]
    public void ParseAuthJson_WithOpencodeGoKey_ExtractsApiKeyAndSource()
    {

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(SAMPLE_OPENCODE_GO_JSON);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_go_12345");
        credential.Source.Should().Be("auth.json:opencode-go");
    }

    /// <summary>Verifies that ParseAuthJson falls back to opencode when opencode-go is absent.</summary>
    [Fact]
    public void ParseAuthJson_WithOpencodeFallback_ExtractsApiKeyAndSource()
    {

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(SAMPLE_OPENCODE_FALLBACK_JSON);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_fallback_67890");
        credential.Source.Should().Be("auth.json:opencode");
    }

    /// <summary>Verifies that ParseAuthJson prioritizes opencode-go over opencode.</summary>
    [Fact]
    public void ParseAuthJson_WithBothKeys_PrioritizesOpencodeGo()
    {

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(SAMPLE_BOTH_KEYS_JSON);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_primary");
        credential.Source.Should().Be("auth.json:opencode-go");
    }

    /// <summary>Verifies that ParseAuthJson falls back when opencode-go key is blank.</summary>
    [Fact]
    public void ParseAuthJson_WithEmptyOpencodeGoKey_FallsBackToOpencode()
    {

        const string json = """
            {
              "opencode-go": { "type": "api", "key": "   " },
              "opencode": { "type": "api", "key": "zen_fallback" }
            }
            """;

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(json);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_fallback");
        credential.Source.Should().Be("auth.json:opencode");
    }

    /// <summary>Verifies that ParseAuthJson extracts string-valued keys directly.</summary>
    [Fact]
    public void ParseAuthJson_WithStringKeyValue_ExtractsApiKey()
    {

        const string json = """{ "opencode-go": "zen_string_value" }""";

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(json);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_string_value");
    }

    /// <summary>Verifies that ParseAuthJson returns null on invalid or missing key payloads.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ invalid json ")]
    [InlineData("[]")]
    [InlineData("""{ "other-provider": { "key": "123" } }""")]
    [InlineData("""{ "opencode-go": {} }""")]
    public void ParseAuthJson_WithInvalidOrIncompletePayload_ReturnsNull(string? invalidJson)
    {

        var credential = OpenCodeCredentialDiscovery.ParseAuthJson(invalidJson);

        credential.Should().BeNull();
    }

    /// <summary>Verifies OPENCODE_GO_API_KEY environment precedence over generic env and file.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenOpencodeGoEnvVarSet_TakesPrecedence()
    {

        var discovery = new OpenCodeCredentialDiscovery(
            authFilePath: "dummy-path.json",
            environmentReader: static name => name switch
            {
                "OPENCODE_GO_API_KEY" => "zen_env_go",
                "OPENCODE_API_KEY" => "zen_env_generic",
                _ => null
            }
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_env_go");
        credential.Source.Should().Be("environment:OPENCODE_GO_API_KEY");
    }

    /// <summary>Verifies OPENCODE_API_KEY precedence over local auth file.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenGenericEnvVarSet_TakesPrecedenceOverFile()
    {

        var discovery = new OpenCodeCredentialDiscovery(
            authFilePath: "dummy-path.json",
            environmentReader: static name => name switch
            {
                "OPENCODE_API_KEY" => "zen_env_generic",
                _ => null
            }
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_env_generic");
        credential.Source.Should().Be("environment:OPENCODE_API_KEY");
    }

    /// <summary>Verifies that DiscoverAsync loads from file when environment variables are absent.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenNoEnvVars_LoadsFromFile()
    {

        using var tempScope = new TempFileScope(SAMPLE_OPENCODE_GO_JSON);

        var discovery = new OpenCodeCredentialDiscovery(
            tempScope.FilePath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_go_12345");
        credential.Source.Should().Be("auth.json:opencode-go");
    }

    /// <summary>Verifies that DiscoverAsync returns null when file is missing and env is absent.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenFileNotFound_ReturnsNull()
    {

        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json");

        var discovery = new OpenCodeCredentialDiscovery(
            nonExistentPath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().BeNull();
    }

    /// <summary>Verifies that DiscoverAsync allows non-locking shared read access while file is open for writing.</summary>
    [Fact]
    public async Task DiscoverAsync_WithConcurrentWriter_ReadsWithoutLockConflict()
    {

        using var tempScope = new TempFileScope(SAMPLE_OPENCODE_GO_JSON);

        using var writeHandle = new FileStream(
            tempScope.FilePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite | FileShare.Delete
        );

        var discovery = new OpenCodeCredentialDiscovery(
            tempScope.FilePath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_go_12345");
    }

    /// <summary>Verifies that DiscoverAsync respects cancellation requests.</summary>
    [Fact]
    public async Task DiscoverAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        var discovery = new OpenCodeCredentialDiscovery();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await discovery.DiscoverAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that default AuthFilePath targets the standard local share location.</summary>
    [Fact]
    public void AuthFilePath_WhenNotSpecified_TargetsDefaultLocation()
    {

        var discovery = new OpenCodeCredentialDiscovery();

        discovery.AuthFilePath.Should().NotBeNullOrWhiteSpace();
        discovery.AuthFilePath.Should().EndWith(Path.Combine(".local", "share", "opencode", "auth.json"));
    }

    /// <summary>Verifies that DiscoverCredentialAsync alias delegates to DiscoverAsync.</summary>
    [Fact]
    public async Task DiscoverCredentialAsync_Alias_DelegatesSuccessfully()
    {

        using var tempScope = new TempFileScope(SAMPLE_OPENCODE_FALLBACK_JSON);

        var discovery = new OpenCodeCredentialDiscovery(
            tempScope.FilePath,
            environmentReader: static _ => null
        );

        var credential = await discovery.DiscoverCredentialAsync(TestContext.Current.CancellationToken);

        credential.Should().NotBeNull();
        credential!.ApiKey.Should().Be("zen_live_fallback_67890");
    }

    private sealed class TempFileScope : IDisposable
    {
        public TempFileScope(string content)
        {

            var dir = Path.Combine(Path.GetTempPath(), $"opencode_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "auth.json");
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
