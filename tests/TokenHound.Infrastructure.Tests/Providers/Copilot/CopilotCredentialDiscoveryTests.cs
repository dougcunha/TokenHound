using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>Verifies strict, read-only Copilot credential precedence.</summary>
public sealed class CopilotCredentialDiscoveryTests
{
    [Fact]
    public async Task DiscoverAsync_UsesEnvironmentPrecedenceAndDoesNotProbeLowerSources()
    {
        var calls = new List<string>();
        var store = new RecordingCredentialStore(calls);
        var discovery = new CopilotCredentialDiscovery(
            store,
            environmentReader: name => name switch
            {
                "COPILOT_GITHUB_TOKEN" => " copilot-token ",
                "GH_TOKEN" => "gh-token",
                _ => null
            },
            ghTokenReader: _ =>
            {
                calls.Add("gh");
                return ValueTask.FromResult<string?>("gh-command-token");
            });

        var result = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("copilot-token", result!.AccessToken);
        Assert.Equal("environment:COPILOT_GITHUB_TOKEN", result.Source);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task DiscoverAsync_UsesGhBeforeKeychainAndConfig()
    {
        var calls = new List<string>();
        var discovery = CreateDiscovery(
            store: new RecordingCredentialStore(calls),
            ghTokenReader: _ =>
            {
                calls.Add("gh");
                return ValueTask.FromResult<string?>("gh-token");
            });

        var result = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal("gh-token", result!.AccessToken);
        Assert.Equal("gh auth token", result.Source);
        Assert.Equal(["gh"], calls);
    }

    [Fact]
    public async Task DiscoverAsync_DerivesQualifiedKeychainTargetsFromValidatedState()
    {
        var calls = new List<string>();
        var store = new RecordingCredentialStore(calls);
        var config = new TestConfigScope(
            """
            {
              "lastLoggedInUser": { "host": "https://ghe.example", "login": "alice" },
              "loggedInUsers": {}
            }
            """);

        store.Values["https://ghe.example:alice.copilot-cli"] = "keychain-token";
        var discovery = CreateDiscovery(
            store,
            new CopilotConfigReader(config.DirectoryPath),
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));

        var result = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal("keychain-token", result!.AccessToken);
        Assert.Contains("copilot-cli", calls);
        Assert.Contains("https://ghe.example:alice.copilot-cli", calls);
    }

    [Fact]
    public async Task DiscoverAsync_UsesExplicitConfigFallbackOnlyAfterKeychainFails()
    {
        var config = new TestConfigScope("{\"oauthToken\":\"config-token\"}");
        var discovery = CreateDiscovery(
            new RecordingCredentialStore([]),
            new CopilotConfigReader(config.DirectoryPath, "oauthToken"),
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));

        var result = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal("config-token", result!.AccessToken);
        Assert.Equal("Copilot CLI config", result.Source);
    }

    [Fact]
    public async Task DiscoverAsync_WhenNoSourceIsUsableReturnsNull()
    {
        var discovery = CreateDiscovery(
            new RecordingCredentialStore([]),
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));

        var result = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private static CopilotCredentialDiscovery CreateDiscovery(
        ICredentialStore store,
        CopilotConfigReader? configReader = null,
        Func<CancellationToken, ValueTask<string?>>? ghTokenReader = null)
        => new(
            store,
            configReader: configReader,
            environmentReader: static _ => null,
            ghTokenReader: ghTokenReader ?? (static _ => ValueTask.FromResult<string?>(null)));

    private sealed class RecordingCredentialStore(List<string> calls) : ICredentialStore
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ValueTask<string?> ReadCredentialAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            calls.Add(target);
            Values.TryGetValue(target, out var value);
            return ValueTask.FromResult<string?>(value);
        }
    }

    private sealed class TestConfigScope : IDisposable
    {
        public TestConfigScope(string content)
        {
            DirectoryPath = global::System.IO.Path.Combine(
                global::System.IO.Path.GetTempPath(),
                $"copilot_discovery_{Guid.NewGuid():N}");
            global::System.IO.Directory.CreateDirectory(DirectoryPath);
            global::System.IO.File.WriteAllText(
                global::System.IO.Path.Combine(DirectoryPath, CopilotConfigReader.CONFIG_FILE_NAME),
                content);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (global::System.IO.Directory.Exists(DirectoryPath))
                global::System.IO.Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
