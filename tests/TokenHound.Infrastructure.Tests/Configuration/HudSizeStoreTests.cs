using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies persistence of <see cref="HudSizeStore"/> and its isolation from sibling sections (TC-02, TC-03).
/// </summary>
public sealed class HudSizeStoreTests : IDisposable
{
    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": { "Left": 100.0, "Top": 200.0 },
      "Refresh": { "ActiveIntervalSeconds": 60 },
      "Log": { "MinimumLevel": "Debug" }
    }
    """;

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudSizeStoreTests"/> class.
    /// </summary>
    public HudSizeStoreTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"hud_size_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that an absent section and a missing file read as the default size.
    /// </summary>
    [Fact]
    public void Load_WhenSectionOrFileIsAbsent_ReturnsDefaultSize()
    {

        var withoutSection = CreateStore("""{"Log":{"MinimumLevel":"Debug"}}""");
        var withoutFile = new HudSizeStore("absent.json", _directoryPath);

        withoutSection.Load().ResolvedPercent.Should().Be(HudSizeSettings.DEFAULT_PERCENT);
        withoutFile.Load().ResolvedPercent.Should().Be(HudSizeSettings.DEFAULT_PERCENT);
    }

    /// <summary>
    /// Verifies that a saved size round-trips and that sibling sections survive the write.
    /// </summary>
    [Fact]
    public void Save_PersistsSizeAndPreservesSiblingSections()
    {

        var store = CreateStore(SETTINGS_WITH_SIBLING_SECTIONS_JSON);

        store.Save(new HudSizeSettings { Percent = 70 }).Should().BeTrue();

        var json = File.ReadAllText(store.FilePath);

        store.Load().ResolvedPercent.Should().Be(70);
        json.Should().Contain("\"Left\"").And.Contain("\"ActiveIntervalSeconds\"").And.Contain("\"MinimumLevel\"");
    }

    /// <summary>
    /// Verifies that persisting the HUD position afterwards does not change the stored size.
    /// </summary>
    [Fact]
    public void SavingHudPosition_DoesNotChangeStoredSize()
    {

        var store = CreateStore("""{"HudSize":{"Percent":60}}""");
        var positionStore = new HudPositionStore(Path.GetFileName(store.FilePath), _directoryPath);

        positionStore.Save(new HudPositionSettings { Left = 10, Top = 20 }).Should().BeTrue();

        store.Load().ResolvedPercent.Should().Be(60);
    }

    private HudSizeStore CreateStore(string json)
    {

        var fileName = $"{Guid.NewGuid():N}.json";

        File.WriteAllText(Path.Combine(_directoryPath, fileName), json);

        return new HudSizeStore(fileName, _directoryPath);
    }
}
