using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>Verifies JSONC parsing and read-only Copilot config behavior.</summary>
public sealed class CopilotConfigReaderTests
{
    [Fact]
    public void ParseToken_RequiresAnExplicitAllowlistedProperty()
    {
        const string json = "{\"oauthToken\":\"fixture-token\",\"nested\":{\"token\":\"wrong\"}}";

        Assert.Null(CopilotConfigReader.ParseToken(json, null));
        Assert.Equal("fixture-token", CopilotConfigReader.ParseToken(json, "oauthToken"));
        Assert.Null(CopilotConfigReader.ParseToken(json, "token"));
    }

    [Fact]
    public void ParseToken_HandlesJsoncCommentsTrailingCommasAndNestedExactPath()
    {
        const string jsonc = """
        {
          // This property name is supplied by the verified fixture.
          "auth": {
            "oauthToken": "fixture-token",
          },
        }
        """;

        Assert.Equal("fixture-token", CopilotConfigReader.ParseToken(jsonc, "auth/oauthToken"));
    }

    [Fact]
    public void ParseLoginState_ExtractsLastAndListedHostLoginPairs()
    {
        const string jsonc = """
        {
          "lastLoggedInUser": { "host": "https://ghe.example", "login": "alice" },
          "loggedInUsers": {
            "one": { "host": "github.com", "login": "octocat" },
            "two": { "host": "https://ghe.example", "login": "alice" }
          }
        }
        """;

        var state = CopilotConfigReader.ParseLoginState(jsonc);

        Assert.NotNull(state);
        Assert.Equal("https://ghe.example", state!.LastLoggedInUser!.Host);
        Assert.Equal("alice", state.LastLoggedInUser.Login);
        Assert.Equal(2, state.LoggedInUsers.Count);
    }

    [Fact]
    public async Task ReadTokenAsync_WhenNoVerifiedPropertyIsConfiguredDoesNotReadAsToken()
    {
        using var scope = new TempConfigScope("{\"oauthToken\":\"fixture-token\"}");
        var reader = new CopilotConfigReader(scope.DirectoryPath);

        var result = await reader.ReadTokenAsync();

        Assert.False(reader.IsPlaintextTokenFallbackEnabled);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadTokenAsync_WithVerifiedPropertyUsesSharedReadAndLeavesFileUnchanged()
    {
        const string json = "{\"oauthToken\":\"fixture-token\"}";
        using var scope = new TempConfigScope(json);
        var reader = new CopilotConfigReader(scope.DirectoryPath, "oauthToken");

        var result = await reader.ReadTokenAsync();

        Assert.Equal("fixture-token", result);
        Assert.Equal(json, await File.ReadAllTextAsync(scope.ConfigPath));
    }

    private sealed class TempConfigScope : IDisposable
    {
        public TempConfigScope(string content)
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"copilot_config_{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            ConfigPath = Path.Combine(DirectoryPath, CopilotConfigReader.CONFIG_FILE_NAME);
            File.WriteAllText(ConfigPath, content);
        }

        public string DirectoryPath { get; }

        public string ConfigPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
