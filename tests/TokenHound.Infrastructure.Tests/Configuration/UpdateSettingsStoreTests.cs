using AwesomeAssertions;
using System;
using System.IO;
using System.Text.Json.Nodes;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies defaults, clamping, round-trip, and section isolation of <see cref="UpdateSettingsStore"/> (TC-07).
/// </summary>
public sealed class UpdateSettingsStoreTests : IDisposable
{
    private const string SETTINGS_WITH_SIBLING_SECTIONS_JSON = """
    {
      "Hud": { "Left": 1599.5, "Top": 240 },
      "Providers": { "claude": { "Enabled": false } },
      "Refresh": { "ActiveIntervalSeconds": 180, "IdleIntervalSeconds": 300 },
      "RateLimit": { "MinimumRetryFloorSeconds": 90 },
      "Log": { "MinimumLevel": "Debug" }
    }
    """;

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsStoreTests"/> class.
    /// </summary>
    public UpdateSettingsStoreTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"update_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a missing section yields enabled checks every 24 hours and no skipped version.
    /// </summary>
    [Fact]
    public void Load_WhenSectionIsAbsent_ReturnsDefaults()
    {

        var settings = CreateStore(SETTINGS_WITH_SIBLING_SECTIONS_JSON).Load();

        settings.IsEnabled.Should().BeTrue();
        settings.IntervalHours.Should().Be(UpdateSettings.DEFAULT_INTERVAL_HOURS);
        settings.SkippedVersion.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a missing or corrupt file yields the defaults.
    /// </summary>
    [Fact]
    public void Load_WhenFileIsMissingOrCorrupt_ReturnsDefaults()
    {

        var missing = new UpdateSettingsStore("absent.json", _directoryPath).Load();
        var corrupt = CreateStore("{ not valid json").Load();

        missing.IntervalHours.Should().Be(24);
        corrupt.IntervalHours.Should().Be(24);
        corrupt.IsEnabled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that out-of-range intervals clamp to 0..720 and that 0 is kept as "periodic checks off".
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, 0)]
    [InlineData(1, 1)]
    [InlineData(720, 720)]
    [InlineData(5000, 720)]
    public void Load_ClampsInterval(int configured, int expected)
    {

        var settings = CreateStore($$$"""{"Update":{"CheckIntervalHours":{{{configured}}}}}""").Load();

        settings.CheckIntervalHours.Should().Be(expected);
        settings.IntervalHours.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that saving round-trips every field.
    /// </summary>
    [Fact]
    public void Save_ThenLoad_RoundTripsAllFields()
    {

        var store = CreateStore("{}");
        var expected = new UpdateSettings { Enabled = false, CheckIntervalHours = 6, SkippedVersion = "1.4.0" };

        store.Save(expected).Should().BeTrue();

        store.Load().Should().Be(expected);
    }

    /// <summary>
    /// Verifies that saving the update section keeps the values of every other section.
    /// </summary>
    [Fact]
    public void Save_PreservesSiblingSections()
    {

        var store = CreateStore(SETTINGS_WITH_SIBLING_SECTIONS_JSON);

        store.Save(new UpdateSettings { CheckIntervalHours = 12 }).Should().BeTrue();

        var after = JsonNode.Parse(File.ReadAllText(store.FilePath))!.AsObject();

        after["Hud"]!["Left"]!.GetValue<double>().Should().Be(1599.5);
        after["Providers"]!["claude"]!["Enabled"]!.GetValue<bool>().Should().BeFalse();
        after["Refresh"]!["ActiveIntervalSeconds"]!.GetValue<int>().Should().Be(180);
        after["Refresh"]!["IdleIntervalSeconds"]!.GetValue<int>().Should().Be(300);
        after["RateLimit"]!["MinimumRetryFloorSeconds"]!.GetValue<int>().Should().Be(90);
        after["Log"]!["MinimumLevel"]!.GetValue<string>().Should().Be("Debug");
        after["Update"]!["CheckIntervalHours"]!.GetValue<int>().Should().Be(12);
    }

    private UpdateSettingsStore CreateStore(string json)
    {

        var path = Path.Combine(_directoryPath, "settings.json");
        File.WriteAllText(path, json);

        return new UpdateSettingsStore(path);
    }
}
