using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies loading and validation behaviors of <see cref="RefreshSettingsStore"/>.
/// </summary>
public sealed class RefreshSettingsStoreTests : IDisposable
{
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

    private RefreshSettingsStore CreateStore(string json)
    {

        var fileName = $"{Guid.NewGuid():N}.json";

        File.WriteAllText(Path.Combine(_directoryPath, fileName), json);

        return new RefreshSettingsStore(fileName, _directoryPath);
    }
}
