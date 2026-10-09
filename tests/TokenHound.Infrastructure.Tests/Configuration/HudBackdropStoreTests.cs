using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies persistence of <see cref="HudBackdropStore"/>, its default, and its isolation from sibling sections (TC-02).
/// </summary>
public sealed class HudBackdropStoreTests : IDisposable
{
    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": { "Left": 100.0, "Top": 200.0 },
      "HudSize": { "Percent": 80 },
      "Log": { "MinimumLevel": "Debug" }
    }
    """;

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudBackdropStoreTests"/> class.
    /// </summary>
    public HudBackdropStoreTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"hud_backdrop_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a missing section, a missing file, and a null value read as enabled.
    /// </summary>
    [Fact]
    public void Load_WhenSectionFileOrValueIsAbsent_ReadsAsEnabled()
    {

        var withoutSection = CreateStore("""{"Log":{"MinimumLevel":"Debug"}}""");
        var withoutFile = new HudBackdropStore("absent.json", _directoryPath);
        var withNull = CreateStore("""{"HudBackdrop":{"Enabled":null}}""");

        withoutSection.Load().IsEnabled.Should().BeTrue();
        withoutFile.Load().IsEnabled.Should().BeTrue();
        withNull.Load().IsEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that turning the background off round-trips and that sibling sections survive the write.
    /// </summary>
    [Fact]
    public void Save_PersistsPreferenceAndPreservesSiblingSections()
    {

        var store = CreateStore(SETTINGS_WITH_SIBLING_SECTIONS_JSON);

        store.Save(new HudBackdropSettings { Enabled = false }).Should().BeTrue();

        var json = File.ReadAllText(store.FilePath);

        store.Load().IsEnabled.Should().BeFalse();
        json.Should().Contain("\"HudBackdrop\"").And.Contain("\"Left\"").And.Contain("\"Percent\"").And.Contain("\"MinimumLevel\"");
    }

    /// <summary>
    /// Verifies that saving the HUD size afterwards does not change the stored preference.
    /// </summary>
    [Fact]
    public void SavingHudSize_DoesNotChangeStoredPreference()
    {

        var store = CreateStore("""{"HudBackdrop":{"Enabled":false}}""");
        var sizeStore = new HudSizeStore(Path.GetFileName(store.FilePath), _directoryPath);

        sizeStore.Save(new HudSizeSettings { Percent = 90 }).Should().BeTrue();

        store.Load().IsEnabled.Should().BeFalse();
    }

    private HudBackdropStore CreateStore(string json)
    {

        var fileName = $"{Guid.NewGuid():N}.json";

        File.WriteAllText(Path.Combine(_directoryPath, fileName), json);

        return new HudBackdropStore(fileName, _directoryPath);
    }
}
