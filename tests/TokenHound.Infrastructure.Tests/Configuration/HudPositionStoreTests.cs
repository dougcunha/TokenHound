using AwesomeAssertions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies persistence and restoration behaviors of <see cref="HudPositionStore"/>.
/// </summary>
public sealed class HudPositionStoreTests : IDisposable
{
    private const string SETTINGS_WITH_LOG_JSON = """
    {
      "Log": {
        "MinimumLevel": "Debug"
      }
    }
    """;

    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": {
        "Left": 100.0,
        "Top": 200.0
      },
      "Providers": {
        "claude": {
          "Enabled": false
        }
      },
      "Refresh": {
        "ActiveIntervalSeconds": 60,
        "IdleIntervalSeconds": 120
      },
      "Log": {
        "MinimumLevel": "Information"
      }
    }
    """;

    private readonly string _directory;
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPositionStoreTests"/> class.
    /// </summary>
    public HudPositionStoreTests()
    {

        _directory = Path.Combine(Path.GetTempPath(), $"tokenhound-hud-{Guid.NewGuid():N}");
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
    public void Load_WhenFileMissing_ReturnsEmptyPosition()
    {

        var store = new HudPositionStore(_filePath);

        var position = store.Load();

        position.TryGetPosition(out _, out _).Should().BeFalse();
        position.Left.Should().BeNull();
        position.Top.Should().BeNull();
    }

    [Fact]
    public void Load_WhenHudSectionMissing_ReturnsEmptyPosition()
    {

        File.WriteAllText(_filePath, SETTINGS_WITH_LOG_JSON);
        var store = new HudPositionStore(_filePath);

        store.Load().TryGetPosition(out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsEmptyPosition()
    {

        File.WriteAllText(_filePath, "{ not json");
        var store = new HudPositionStore(_filePath);

        store.Load().TryGetPosition(out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsPosition()
    {

        var store = new HudPositionStore(_filePath);

        store.Save(new HudPositionSettings { Left = 1599.5, Top = 240 }).Should().BeTrue();

        var restored = store.Load();

        restored.TryGetPosition(out var left, out var top).Should().BeTrue();
        left.Should().Be(1599.5);
        top.Should().Be(240);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsPosition()
    {

        var store = new HudPositionStore(_filePath);

        var saved = await store.SaveAsync(
            new HudPositionSettings { Left = 800.0, Top = 400.0 },
            TestContext.Current.CancellationToken);

        saved.Should().BeTrue();

        var restored = await store.LoadAsync(TestContext.Current.CancellationToken);

        restored.TryGetPosition(out var left, out var top).Should().BeTrue();
        left.Should().Be(800.0);
        top.Should().Be(400.0);
    }

    [Fact]
    public void Save_PreservesOtherSections()
    {

        File.WriteAllText(_filePath, SETTINGS_WITH_LOG_JSON);
        var store = new HudPositionStore(_filePath);

        store.Save(new HudPositionSettings { Left = 10, Top = 20 }).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
        var root = document.RootElement;

        root.GetProperty("Log").GetProperty("MinimumLevel").GetString().Should().Be("Debug");
        root.GetProperty("Hud").GetProperty("Left").GetDouble().Should().Be(10);
    }

    [Fact]
    public void Save_WhenSiblingSectionsExist_UpdatesOnlyHudSection()
    {

        File.WriteAllText(_filePath, SETTINGS_WITH_SIBLING_SECTIONS_JSON);
        var store = new HudPositionStore(_filePath);

        var saved = store.Save(new HudPositionSettings { Left = 350.0, Top = 450.0 });

        saved.Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
        var root = document.RootElement;

        root.GetProperty("Hud").GetProperty("Left").GetDouble().Should().Be(350.0);
        root.GetProperty("Hud").GetProperty("Top").GetDouble().Should().Be(450.0);
        root.GetProperty("Providers").GetProperty("claude").GetProperty("Enabled").GetBoolean().Should().BeFalse();
        root.GetProperty("Refresh").GetProperty("ActiveIntervalSeconds").GetInt32().Should().Be(60);
        root.GetProperty("Refresh").GetProperty("IdleIntervalSeconds").GetInt32().Should().Be(120);
        root.GetProperty("Log").GetProperty("MinimumLevel").GetString().Should().Be("Information");
    }

    [Fact]
    public void Constructor_WithUserSettingsFile_DelegatesPersistence()
    {

        var settingsFile = new UserSettingsFile(_filePath);
        var store = new HudPositionStore(settingsFile);

        store.Save(new HudPositionSettings { Left = 50.0, Top = 75.0 }).Should().BeTrue();

        var reloaded = store.Load();

        reloaded.Left.Should().Be(50.0);
        reloaded.Top.Should().Be(75.0);
    }

    [Fact]
    public void Save_WhenPositionIsCleared_WritesNullCoordinates()
    {

        var store = new HudPositionStore(_filePath);

        store.Save(new HudPositionSettings()).Should().BeTrue();

        store.Load().TryGetPosition(out _, out _).Should().BeFalse();
    }

    [Fact]
    public void FromJson_WhenCoordinatesAreNotFinite_ReportsNoPosition()
    {

        var position = HudPositionStore.FromJson("""{ "Hud": { "Left": 10, "Top": null } }""");

        position.TryGetPosition(out _, out _).Should().BeFalse();
    }
}
