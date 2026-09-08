using AwesomeAssertions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies loading, saving, and validation behaviors of <see cref="RefreshSettingsStore"/>.
/// </summary>
public sealed class RefreshSettingsStoreTests : IDisposable
{
    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": {
        "Left": 1599.5,
        "Top": 240
      },
      "Providers": {
        "claude": {
          "Enabled": false
        }
      },
      "Refresh": {
        "ActiveIntervalSeconds": 180,
        "IdleIntervalSeconds": 300
      },
      "Log": {
        "MinimumLevel": "Debug"
      }
    }
    """;

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStoreTests"/> class.
    /// </summary>
    public RefreshSettingsStoreTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"refresh_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a configured cadence is read from the refresh section.
    /// </summary>
    [Fact]
    public void Load_WhenRefreshSectionIsPresent_ReturnsConfiguredIntervals()
    {

        var store = CreateStore("""{"Refresh":{"ActiveIntervalSeconds":240,"IdleIntervalSeconds":600}}""");

        var settings = store.Load();

        settings.ActiveInterval.Should().Be(TimeSpan.FromSeconds(240));
        settings.IdleInterval.Should().Be(TimeSpan.FromSeconds(600));
    }

    /// <summary>
    /// Verifies that an absent refresh section falls back to the schedule policy defaults.
    /// </summary>
    [Fact]
    public void Load_WhenRefreshSectionIsAbsent_ReturnsPolicyDefaults()
    {

        var store = CreateStore("""{"Log":{"MinimumLevel":"Debug"}}""");

        var settings = store.Load();

        settings.ActiveInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL);
        settings.IdleInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL);
    }

    /// <summary>
    /// Verifies that intervals below the accepted minimum are rejected in favor of the defaults.
    /// </summary>
    [Fact]
    public void Load_WhenIntervalsAreBelowMinimum_ReturnsPolicyDefaults()
    {

        var store = CreateStore("""{"Refresh":{"ActiveIntervalSeconds":5,"IdleIntervalSeconds":0}}""");

        var settings = store.Load();

        settings.ActiveInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL);
        settings.IdleInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL);
    }

    /// <summary>
    /// Verifies that a missing settings file yields the schedule policy defaults.
    /// </summary>
    [Fact]
    public void Load_WhenFileIsMissing_ReturnsPolicyDefaults()
    {

        var store = new RefreshSettingsStore("absent.json", _directoryPath);

        var settings = store.Load();

        settings.ActiveIntervalSeconds.Should().BeNull();
        settings.ActiveInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL);
    }

    /// <summary>
    /// Verifies that malformed JSON is tolerated and yields the schedule policy defaults.
    /// </summary>
    [Fact]
    public void Load_WhenJsonIsMalformed_ReturnsPolicyDefaults()
    {

        var store = CreateStore("{ not json");

        var settings = store.Load();

        settings.ActiveInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL);
        settings.IdleInterval.Should().Be(RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL);
    }

    /// <summary>
    /// Verifies that saving refresh settings persists active and idle intervals.
    /// </summary>
    [Fact]
    public void Save_ThenLoad_RoundTripsIntervals()
    {

        var filePath = Path.Combine(_directoryPath, "save_test.json");
        var store = new RefreshSettingsStore(filePath);
        var expected = new RefreshSettings { ActiveIntervalSeconds = 45, IdleIntervalSeconds = 90 };

        store.Save(expected).Should().BeTrue();

        var restored = store.Load();

        restored.ActiveIntervalSeconds.Should().Be(45);
        restored.IdleIntervalSeconds.Should().Be(90);
        restored.ActiveInterval.Should().Be(TimeSpan.FromSeconds(45));
        restored.IdleInterval.Should().Be(TimeSpan.FromSeconds(90));
    }

    /// <summary>
    /// Verifies that saving refresh settings preserves sibling sections.
    /// </summary>
    [Fact]
    public void Save_WhenSiblingSectionsExist_UpdatesOnlyRefreshSection()
    {

        var filePath = Path.Combine(_directoryPath, "siblings_test.json");
        File.WriteAllText(filePath, SETTINGS_WITH_SIBLING_SECTIONS_JSON);
        var store = new RefreshSettingsStore(filePath);

        var saved = store.Save(new RefreshSettings { ActiveIntervalSeconds = 60, IdleIntervalSeconds = 120 });

        saved.Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        var root = document.RootElement;

        root.GetProperty("Hud").GetProperty("Left").GetDouble().Should().Be(1599.5);
        root.GetProperty("Providers").GetProperty("claude").GetProperty("Enabled").GetBoolean().Should().BeFalse();
        root.GetProperty("Refresh").GetProperty("ActiveIntervalSeconds").GetInt32().Should().Be(60);
        root.GetProperty("Refresh").GetProperty("IdleIntervalSeconds").GetInt32().Should().Be(120);
        root.GetProperty("Log").GetProperty("MinimumLevel").GetString().Should().Be("Debug");
    }

    /// <summary>
    /// Verifies that asynchronous save round-trips intervals successfully.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsIntervals()
    {

        var filePath = Path.Combine(_directoryPath, "save_async_test.json");
        var store = new RefreshSettingsStore(filePath);
        var expected = new RefreshSettings { ActiveIntervalSeconds = 50, IdleIntervalSeconds = 100 };

        var saved = await store.SaveAsync(expected, TestContext.Current.CancellationToken);

        saved.Should().BeTrue();

        var restored = await store.LoadAsync(TestContext.Current.CancellationToken);

        restored.ActiveIntervalSeconds.Should().Be(50);
        restored.IdleIntervalSeconds.Should().Be(100);
    }

    /// <summary>
    /// Verifies that a store backed by a custom <see cref="UserSettingsFile"/> persists properly.
    /// </summary>
    [Fact]
    public void Constructor_WithUserSettingsFile_DelegatesPersistence()
    {

        var filePath = Path.Combine(_directoryPath, "user_settings_file_test.json");
        var settingsFile = new UserSettingsFile(filePath);
        var store = new RefreshSettingsStore(settingsFile);

        store.Save(new RefreshSettings { ActiveIntervalSeconds = 35, IdleIntervalSeconds = 70 }).Should().BeTrue();

        var loaded = store.Load();

        loaded.ActiveIntervalSeconds.Should().Be(35);
        loaded.IdleIntervalSeconds.Should().Be(70);
    }

    /// <summary>
    /// Verifies that <see cref="RefreshSettingsStore.FromJson"/> parses valid refresh section JSON.
    /// </summary>
    [Fact]
    public void FromJson_WhenValidJson_ParsesRefreshIntervals()
    {

        var settings = RefreshSettingsStore.FromJson("""{"Refresh":{"ActiveIntervalSeconds":200,"IdleIntervalSeconds":400}}""");

        settings.ActiveIntervalSeconds.Should().Be(200);
        settings.IdleIntervalSeconds.Should().Be(400);
    }

    private RefreshSettingsStore CreateStore(string json)
    {

        var fileName = $"{Guid.NewGuid():N}.json";

        File.WriteAllText(Path.Combine(_directoryPath, fileName), json);

        return new RefreshSettingsStore(fileName, _directoryPath);
    }
}
