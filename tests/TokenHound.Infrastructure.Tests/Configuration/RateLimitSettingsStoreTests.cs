using AwesomeAssertions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies loading, saving, clamping, and isolation behaviors of <see cref="RateLimitSettingsStore"/>.
/// </summary>
public sealed class RateLimitSettingsStoreTests : IDisposable
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
      "RateLimit": {
        "MinimumRetryFloorSeconds": 90
      },
      "Log": {
        "MinimumLevel": "Debug"
      }
    }
    """;

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettingsStoreTests"/> class.
    /// </summary>
    public RateLimitSettingsStoreTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"ratelimit_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that an absent rate limit section returns the default 60-second floor.
    /// </summary>
    [Fact]
    public void Load_WhenRateLimitSectionIsAbsent_ReturnsDefaultFloor()
    {

        var store = CreateStore("""{"Log":{"MinimumLevel":"Debug"}}""");

        var settings = store.Load();

        settings.MinimumRetryFloorSeconds.Should().BeNull();
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(RateLimitSettings.DEFAULT_FLOOR_SECONDS));
        settings.MinimumRetryFloor.TotalSeconds.Should().Be(60);
    }

    /// <summary>
    /// Verifies that a missing settings file yields the default 60-second floor.
    /// </summary>
    [Fact]
    public void Load_WhenFileIsMissing_ReturnsDefaultFloor()
    {

        var store = new RateLimitSettingsStore("absent.json", _directoryPath);

        var settings = store.Load();

        settings.MinimumRetryFloorSeconds.Should().BeNull();
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(RateLimitSettings.DEFAULT_FLOOR_SECONDS));
    }

    /// <summary>
    /// Verifies that malformed JSON yields the default 60-second floor.
    /// </summary>
    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsDefaultFloor()
    {

        var store = CreateStore("{ not valid json");

        var settings = store.Load();

        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(RateLimitSettings.DEFAULT_FLOOR_SECONDS));
    }

    /// <summary>
    /// Verifies that a configured value below 60 seconds is clamped to 60 seconds on load.
    /// </summary>
    [Fact]
    public void Load_WhenValueIsBelowMinimumFloor_ClampsToMinimumFloor()
    {

        var store = CreateStore("""{"RateLimit":{"MinimumRetryFloorSeconds":15}}""");

        var settings = store.Load();

        settings.MinimumRetryFloorSeconds.Should().Be(RateLimitSettings.MINIMUM_FLOOR_SECONDS);
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that a configured value at or above 60 seconds is preserved on load.
    /// </summary>
    [Fact]
    public void Load_WhenValueIsAboveMinimumFloor_ReturnsConfiguredFloor()
    {

        var store = CreateStore("""{"RateLimit":{"MinimumRetryFloorSeconds":120}}""");

        var settings = store.Load();

        settings.MinimumRetryFloorSeconds.Should().Be(120);
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>
    /// Verifies that saving rate limit settings round-trips a valid floor duration.
    /// </summary>
    [Fact]
    public void Save_ThenLoad_RoundTripsCustomValue()
    {

        var filePath = Path.Combine(_directoryPath, "save_test.json");
        var store = new RateLimitSettingsStore(filePath);
        var expected = new RateLimitSettings { MinimumRetryFloorSeconds = 90 };

        store.Save(expected).Should().BeTrue();

        var restored = store.Load();

        restored.MinimumRetryFloorSeconds.Should().Be(90);
        restored.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(90));
    }

    /// <summary>
    /// Verifies that saving a floor value below 60 seconds clamps to 60 seconds.
    /// </summary>
    [Fact]
    public void Save_WhenValueIsBelowMinimumFloor_ClampsToMinimumFloor()
    {

        var filePath = Path.Combine(_directoryPath, "clamp_test.json");
        var store = new RateLimitSettingsStore(filePath);
        var subFloor = new RateLimitSettings { MinimumRetryFloorSeconds = 25 };

        store.Save(subFloor).Should().BeTrue();

        var restored = store.Load();

        restored.MinimumRetryFloorSeconds.Should().Be(RateLimitSettings.MINIMUM_FLOOR_SECONDS);
        restored.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that saving rate limit settings preserves all sibling sections.
    /// </summary>
    [Fact]
    public void Save_WhenSiblingSectionsExist_UpdatesOnlyRateLimitSection()
    {

        var filePath = Path.Combine(_directoryPath, "siblings_test.json");
        File.WriteAllText(filePath, SETTINGS_WITH_SIBLING_SECTIONS_JSON);
        var store = new RateLimitSettingsStore(filePath);

        var saved = store.Save(new RateLimitSettings { MinimumRetryFloorSeconds = 150 });

        saved.Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        var root = document.RootElement;

        root.GetProperty("Hud").GetProperty("Left").GetDouble().Should().Be(1599.5);
        root.GetProperty("Providers").GetProperty("claude").GetProperty("Enabled").GetBoolean().Should().BeFalse();
        root.GetProperty("Refresh").GetProperty("ActiveIntervalSeconds").GetInt32().Should().Be(180);
        root.GetProperty("RateLimit").GetProperty("MinimumRetryFloorSeconds").GetInt32().Should().Be(150);
        root.GetProperty("Log").GetProperty("MinimumLevel").GetString().Should().Be("Debug");
    }

    /// <summary>
    /// Verifies that asynchronous saving round-trips configuration successfully.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {

        var filePath = Path.Combine(_directoryPath, "save_async_test.json");
        var store = new RateLimitSettingsStore(filePath);
        var expected = new RateLimitSettings { MinimumRetryFloorSeconds = 150 };

        var saved = await store.SaveAsync(expected, TestContext.Current.CancellationToken);

        saved.Should().BeTrue();

        var restored = await store.LoadAsync(TestContext.Current.CancellationToken);

        restored.MinimumRetryFloorSeconds.Should().Be(150);
        restored.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(150));
    }

    /// <summary>
    /// Verifies that a store backed by a custom <see cref="UserSettingsFile"/> persists properly.
    /// </summary>
    [Fact]
    public void Constructor_WithUserSettingsFile_DelegatesPersistence()
    {

        var filePath = Path.Combine(_directoryPath, "user_settings_file_test.json");
        var settingsFile = new UserSettingsFile(filePath);
        var store = new RateLimitSettingsStore(settingsFile);

        store.Save(new RateLimitSettings { MinimumRetryFloorSeconds = 100 }).Should().BeTrue();

        var loaded = store.Load();

        loaded.MinimumRetryFloorSeconds.Should().Be(100);
        loaded.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(100));
    }

    /// <summary>
    /// Verifies constructor throws <see cref="ArgumentNullException"/> when settings file is null.
    /// </summary>
    [Fact]
    public void Constructor_WhenSettingsFileIsNull_ThrowsArgumentNullException()
    {

        var action = static () => new RateLimitSettingsStore((UserSettingsFile)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.Save"/> throws <see cref="ArgumentNullException"/> when settings is null.
    /// </summary>
    [Fact]
    public void Save_WhenSettingsIsNull_ThrowsArgumentNullException()
    {

        var store = new RateLimitSettingsStore(new UserSettingsFile(Path.Combine(_directoryPath, "dummy.json")));

        var action = () => store.Save(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.SaveAsync"/> throws <see cref="ArgumentNullException"/> when settings is null.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WhenSettingsIsNull_ThrowsArgumentNullException()
    {

        var store = new RateLimitSettingsStore(new UserSettingsFile(Path.Combine(_directoryPath, "dummy.json")));

        var action = () => store.SaveAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.FromJson"/> parses valid rate limit JSON.
    /// </summary>
    [Fact]
    public void FromJson_WhenValidJson_ParsesConfiguredFloor()
    {

        var settings = RateLimitSettingsStore.FromJson("""{"RateLimit":{"MinimumRetryFloorSeconds":180}}""");

        settings.MinimumRetryFloorSeconds.Should().Be(180);
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(180));
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.FromJson"/> clamps values below 60 seconds.
    /// </summary>
    [Fact]
    public void FromJson_WhenSubFloorJson_ClampsToMinimumFloor()
    {

        var settings = RateLimitSettingsStore.FromJson("""{"RateLimit":{"MinimumRetryFloorSeconds":10}}""");

        settings.MinimumRetryFloorSeconds.Should().Be(RateLimitSettings.MINIMUM_FLOOR_SECONDS);
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.FromJson"/> returns default floor when section is absent.
    /// </summary>
    [Fact]
    public void FromJson_WhenSectionAbsent_ReturnsDefaultFloor()
    {

        var settings = RateLimitSettingsStore.FromJson("""{"Log":{"MinimumLevel":"Debug"}}""");

        settings.MinimumRetryFloorSeconds.Should().BeNull();
        settings.MinimumRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies <see cref="RateLimitSettingsStore.FilePath"/> returns the underlying user settings path.
    /// </summary>
    [Fact]
    public void FilePath_ReturnsUnderlyingUserSettingsPath()
    {

        var expectedPath = Path.Combine(_directoryPath, "custom_path.json");
        var store = new RateLimitSettingsStore(expectedPath);

        store.FilePath.Should().Be(expectedPath);
    }

    private RateLimitSettingsStore CreateStore(string json)
    {

        var fileName = $"{Guid.NewGuid():N}.json";

        File.WriteAllText(Path.Combine(_directoryPath, fileName), json);

        return new RateLimitSettingsStore(fileName, _directoryPath);
    }
}
