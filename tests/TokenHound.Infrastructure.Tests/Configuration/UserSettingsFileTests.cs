using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies atomic persistence, defaults fallback, migration, and concurrency of <see cref="UserSettingsFile"/>.
/// </summary>
public sealed class UserSettingsFileTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _userSettingsPath;
    private readonly string _defaultsFilePath;

    /// <summary>Initializes a new instance of the <see cref="UserSettingsFileTests"/> class.</summary>
    public UserSettingsFileTests()
    {

        _testDirectory = Path.Combine(Path.GetTempPath(), $"tokenhound-settings-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        _userSettingsPath = Path.Combine(_testDirectory, "user", "settings.json");
        _defaultsFilePath = Path.Combine(_testDirectory, "appsettings.json");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Fact]
    public void DefaultUserSettingsPath_ResolvesUnderLocalAppData()
    {

        UserSettingsFile.DefaultUserSettingsPath.Should().Contain("TokenHound");
        UserSettingsFile.DefaultUserSettingsPath.Should().EndWith("settings.json");
    }

    [Fact]
    public void Load_WhenUserSettingsFileMissing_ReturnsDefaultsWithoutCreatingUserFile()
    {

        File.WriteAllText(_defaultsFilePath, """
        {
          "Hud": { "Left": null, "Top": null },
          "Refresh": { "ActiveIntervalSeconds": 180, "IdleIntervalSeconds": 300 }
        }
        """);

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);

        var loaded = file.Load();

        File.Exists(_userSettingsPath).Should().BeFalse();
        loaded.Hud.Should().NotBeNull();
        loaded.Hud!.Left.Should().BeNull();
        loaded.Refresh!.ActiveIntervalSeconds.Should().Be(180);
    }

    [Fact]
    public void Save_ReplacesFileAtomicallyWithoutLeavingTempFile()
    {

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);
        var settings = new UserSettings { Hud = new HudPositionSettings { Left = 100.5, Top = 200.0 } };

        var saved = file.Save(settings);

        saved.Should().BeTrue();
        File.Exists(_userSettingsPath).Should().BeTrue();
        File.Exists($"{_userSettingsPath}.tmp").Should().BeFalse();

        var reloaded = file.Load();

        reloaded.Hud!.Left.Should().Be(100.5);
        reloaded.Hud.Top.Should().Be(200.0);
    }

    [Fact]
    public void Save_WhenDirectoryDoesNotExist_CreatesDirectoryAndPersists()
    {

        var nestedPath = Path.Combine(_testDirectory, "nested", "sub", "settings.json");
        var file = new UserSettingsFile(nestedPath, _defaultsFilePath);

        var saved = file.Save(new UserSettings());

        saved.Should().BeTrue();
        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public void Load_WhenDefaultsCustomizedAndUserFileMissing_MigratesCustomSettings()
    {

        File.WriteAllText(_defaultsFilePath, """
        {
          "Hud": { "Left": 550.0, "Top": 220.0 },
          "Providers": { "claude": { "Enabled": false }, "gemini": { "Enabled": true } }
        }
        """);

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);

        var loaded = file.Load();

        File.Exists(_userSettingsPath).Should().BeTrue();
        loaded.Hud!.Left.Should().Be(550.0);
        loaded.Hud.Top.Should().Be(220.0);
        loaded.Providers!.IsEnabled("claude").Should().BeFalse();
        loaded.Providers.IsEnabled("gemini").Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhenCalledConcurrently_MergesSectionsWithoutDataLoss()
    {

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);

        var ct = TestContext.Current.CancellationToken;

        var taskHud = Task.Run(() => file.UpdateAsync(static s => s with
        {
            Hud = new HudPositionSettings { Left = 123.0, Top = 456.0 }
        }, ct), ct);

        var taskProviders = Task.Run(() => file.UpdateAsync(static s => s with
        {
            Providers = new ProviderSettings
            {
                EnabledStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["claude"] = false
                }
            }
        }, ct), ct);

        var taskRefresh = Task.Run(() => file.UpdateAsync(static s => s with
        {
            Refresh = new RefreshSettings { ActiveIntervalSeconds = 45, IdleIntervalSeconds = 90 }
        }, ct), ct);

        var taskRateLimit = Task.Run(() => file.UpdateAsync(static s => s with
        {
            RateLimit = new RateLimitSettings { MinimumRetryFloorSeconds = 120 }
        }, ct), ct);

        var results = await Task.WhenAll(taskHud, taskProviders, taskRefresh, taskRateLimit);

        results.Should().AllSatisfy(static r => r.Should().BeTrue());

        var restored = await file.LoadAsync(ct);

        restored.Hud!.Left.Should().Be(123.0);
        restored.Hud.Top.Should().Be(456.0);
        restored.Providers!.IsEnabled("claude").Should().BeFalse();
        restored.Refresh!.ActiveIntervalSeconds.Should().Be(45);
        restored.RateLimit!.MinimumRetryFloorSeconds.Should().Be(120);
    }

    [Fact]
    public void Load_WhenUserFileCorrupt_FallsBackToDefaultsWithoutThrowing()
    {

        Directory.CreateDirectory(Path.GetDirectoryName(_userSettingsPath)!);
        File.WriteAllText(_userSettingsPath, "{ corrupted json");
        File.WriteAllText(_defaultsFilePath, """{ "Refresh": { "ActiveIntervalSeconds": 240 } }""");

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);

        var loaded = file.Load();

        loaded.Should().NotBeNull();
        loaded.Refresh!.ActiveIntervalSeconds.Should().Be(240);
    }

    [Fact]
    public void RateLimitSettings_ClampsMinimumRetryFloorToSixtySeconds()
    {

        var defaultFloor = new RateLimitSettings();
        var clampedFloor = new RateLimitSettings { MinimumRetryFloorSeconds = 10 };
        var customFloor = new RateLimitSettings { MinimumRetryFloorSeconds = 180 };

        defaultFloor.MinimumRetryFloor.TotalSeconds.Should().Be(60);
        clampedFloor.MinimumRetryFloor.TotalSeconds.Should().Be(60);
        customFloor.MinimumRetryFloor.TotalSeconds.Should().Be(180);
    }

    [Fact]
    public void Update_PreservesSiblingSectionsWhileMutatingTarget()
    {

        var file = new UserSettingsFile(_userSettingsPath, _defaultsFilePath);

        file.Save(new UserSettings
        {
            Hud = new HudPositionSettings { Left = 50.0, Top = 60.0 },
            Refresh = new RefreshSettings { ActiveIntervalSeconds = 120 }
        });

        var updated = file.Update(static s => s with
        {
            Hud = new HudPositionSettings { Left = 80.0, Top = 90.0 }
        });

        updated.Should().BeTrue();

        var loaded = file.Load();

        loaded.Hud!.Left.Should().Be(80.0);
        loaded.Hud.Top.Should().Be(90.0);
        loaded.Refresh!.ActiveIntervalSeconds.Should().Be(120);
    }
}
