using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies persistence, defaulting, and alias behaviors of <see cref="ProviderSettingsStore"/>.
/// </summary>
public sealed class ProviderSettingsStoreTests : IDisposable
{
    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": {
        "Left": 1599.5,
        "Top": 240
      },
      "Refresh": {
        "ActiveIntervalSeconds": 180,
        "IdleIntervalSeconds": 900
      },
      "Log": {
        "MinimumLevel": "Debug"
      }
    }
    """;

    private readonly string _directory;
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSettingsStoreTests"/> class.
    /// </summary>
    public ProviderSettingsStoreTests()
    {

        _directory = Path.Combine(Path.GetTempPath(), $"tokenhound-providers-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "appsettings.json");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_PreservesSiblingSectionsAndRoundTrips()
    {

        File.WriteAllText(_filePath, SETTINGS_WITH_SIBLING_SECTIONS_JSON);
        var store = new ProviderSettingsStore(_filePath);
        var settings = BuildSettings(("claude", true), ("gemini", false));

        var saved = await store.SaveAsync(settings, TestContext.Current.CancellationToken);

        saved.Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
        var root = document.RootElement;

        root.GetProperty("Hud").GetProperty("Left").GetDouble().Should().Be(1599.5);
        root.GetProperty("Refresh").GetProperty("IdleIntervalSeconds").GetInt32().Should().Be(900);
        root.GetProperty("Log").GetProperty("MinimumLevel").GetString().Should().Be("Debug");

        var restored = store.Load();

        restored.IsEnabled("claude").Should().BeTrue();
        restored.IsEnabled("gemini").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_PreservesProviderKeysUnknownToThisBuild()
    {

        File.WriteAllText(_filePath, """{ "Providers": { "future": { "Enabled": false } } }""");
        var store = new ProviderSettingsStore(_filePath);

        await store.SaveAsync(BuildSettings(("claude", false)), TestContext.Current.CancellationToken);

        var restored = store.Load();

        restored.IsEnabled("future").Should().BeFalse();
        restored.IsEnabled("claude").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WhenCanonicalKeyIsNotSupplied_KeepsStoredAliasPreference()
    {

        File.WriteAllText(_filePath, """{ "Providers": { "antigravity": { "Enabled": false } } }""");
        var store = new ProviderSettingsStore(_filePath);

        await store.SaveAsync(BuildSettings(("claude", true)), TestContext.Current.CancellationToken);

        store.Load().IsEnabled("gemini").Should().BeFalse();

        using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
        var providers = document.RootElement.GetProperty("Providers");

        providers.GetProperty("antigravity").GetProperty("Enabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Load_WhenFileMissing_ReportsEnabled()
    {

        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("claude").Should().BeTrue();
    }

    [Fact]
    public void Load_WhenProvidersSectionMissing_ReportsEnabled()
    {

        File.WriteAllText(_filePath, SETTINGS_WITH_SIBLING_SECTIONS_JSON);
        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("claude").Should().BeTrue();
    }

    [Fact]
    public void Load_WhenProviderKeyAbsent_ReportsEnabled()
    {

        File.WriteAllText(_filePath, """{ "Providers": { "codex": { "Enabled": false } } }""");
        var store = new ProviderSettingsStore(_filePath);

        var settings = store.Load();

        settings.IsEnabled("codex").Should().BeFalse();
        settings.IsEnabled("cursor").Should().BeTrue();
    }

    [Fact]
    public async Task Load_WhenAliasKeyStored_ResolvesAsCanonicalAndRewritesIt()
    {

        File.WriteAllText(_filePath, """{ "Providers": { "antigravity": { "Enabled": false } } }""");
        var store = new ProviderSettingsStore(_filePath);

        var loaded = store.Load();

        loaded.IsEnabled("gemini").Should().BeFalse();

        await store.SaveAsync(loaded, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
        var providers = document.RootElement.GetProperty("Providers");

        providers.TryGetProperty("antigravity", out _).Should().BeFalse();
        providers.GetProperty("gemini").GetProperty("Enabled").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData("""{ "Providers": { "gemini": { "Enabled": true }, "antigravity": { "Enabled": false } } }""")]
    [InlineData("""{ "Providers": { "antigravity": { "Enabled": false }, "gemini": { "Enabled": true } } }""")]
    public void Load_WhenBothAliasAndCanonicalKeysExist_CanonicalWins(string json)
    {

        File.WriteAllText(_filePath, json);
        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("gemini").Should().BeTrue();
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReportsEnabled()
    {

        File.WriteAllText(_filePath, "{ not json");
        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("claude").Should().BeTrue();
    }

    [Theory]
    [InlineData("""{ "Providers": { "claude": { "Enabled": "false" } } }""")]
    [InlineData("""{ "Providers": { "claude": { "Enabled": 0 } } }""")]
    [InlineData("""{ "Providers": { "claude": { "Enabled": null } } }""")]
    [InlineData("""{ "Providers": { "claude": {} } }""")]
    [InlineData("""{ "Providers": { "claude": false } }""")]
    public void Load_WhenEnabledIsNotBoolean_ReportsEnabled(string json)
    {

        File.WriteAllText(_filePath, json);
        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("claude").Should().BeTrue();
    }

    [Fact]
    public void Load_MatchesProviderKeysCaseInsensitively()
    {

        File.WriteAllText(_filePath, """{ "Providers": { "Claude": { "enabled": false } } }""");
        var store = new ProviderSettingsStore(_filePath);

        store.Load().IsEnabled("claude").Should().BeFalse();
    }

    private static ProviderSettings BuildSettings(params (string ProviderId, bool IsEnabled)[] states)
    {

        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var state in states)
            map[state.ProviderId] = state.IsEnabled;

        return new ProviderSettings { EnabledStates = map };
    }
}
