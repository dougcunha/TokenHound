using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies validation, apply, persistence round-trip, and skipped-version preservation of
/// <see cref="UpdateSettingsViewModel"/> against a real store on a temp file (TC-19).
/// </summary>
public sealed class UpdateSettingsViewModelTests : IDisposable
{
    private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), $"update_settings_vm_{Guid.NewGuid():N}");

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>Verifies that a fresh settings file shows the defaults and offers nothing to apply.</summary>
    [Fact]
    public void Constructor_WithoutSettings_ShowsDefaultsAndCannotApply()
    {

        var sut = new UpdateSettingsViewModel(CreateStore());

        sut.IsEnabled.Should().BeTrue();
        sut.IntervalHoursText.Should().Be("24");
        sut.IntervalError.Should().BeNull();
        sut.IsDirty.Should().BeFalse();
        sut.CanApply.Should().BeFalse();
    }

    /// <summary>Verifies that non-integer, negative, and too-large intervals show the error and disable Apply.</summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("-1")]
    [InlineData("721")]
    public void IntervalHoursText_WhenInvalid_ShowsErrorAndDisablesApply(string text)
    {

        var sut = new UpdateSettingsViewModel(CreateStore()) { IntervalHoursText = text };

        sut.IntervalError.Should().Be(UpdateSettingsViewModel.INTERVAL_ERROR);
        sut.CanApply.Should().BeFalse();
        sut.ApplyCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>Verifies that Apply writes the flag and interval, keeps the skipped version, and survives a reload.</summary>
    [Fact]
    public void Apply_PersistsValuesKeepsSkippedVersion_AndReloads()
    {

        var store = CreateStore();
        store.Save(new UpdateSettings { SkippedVersion = "1.4.0" });
        var sut = new UpdateSettingsViewModel(store) { IsEnabled = false, IntervalHoursText = "0" };

        sut.CanApply.Should().BeTrue();
        sut.ApplyCommand.Execute(null);

        var saved = store.Load();
        saved.Enabled.Should().BeFalse();
        saved.CheckIntervalHours.Should().Be(0);
        saved.SkippedVersion.Should().Be("1.4.0");
        sut.IsApplied.Should().BeTrue();
        sut.CanApply.Should().BeFalse();

        var reopened = new UpdateSettingsViewModel(store);

        reopened.IsEnabled.Should().BeFalse();
        reopened.IntervalHoursText.Should().Be("0");
    }

    /// <summary>Verifies that editing after Apply clears the applied notice and re-enables Apply.</summary>
    [Fact]
    public void Edit_AfterApply_ClearsAppliedNotice()
    {

        var sut = new UpdateSettingsViewModel(CreateStore()) { IntervalHoursText = "12" };

        sut.Apply();
        sut.IsApplied.Should().BeTrue();

        sut.IntervalHoursText = "6";

        sut.IsApplied.Should().BeFalse();
        sut.CanApply.Should().BeTrue();
    }

    /// <summary>Verifies that a failing save reports an error, keeps the edits, and does not claim success.</summary>
    [Fact]
    public void Apply_WhenSaveFails_ShowsErrorAndKeepsDirty()
    {

        Directory.CreateDirectory(_directoryPath);
        var blocked = Path.Combine(_directoryPath, "blocked");
        Directory.CreateDirectory(blocked);
        var sut = new UpdateSettingsViewModel(new UpdateSettingsStore(blocked)) { IntervalHoursText = "12" };

        sut.Apply();

        sut.ApplyError.Should().NotBeNull();
        sut.IsApplied.Should().BeFalse();
        sut.IsDirty.Should().BeTrue();
    }

    /// <summary>Verifies that the boundary values 0 and 720 are accepted.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("720")]
    public void IntervalHoursText_AtBounds_IsValid(string text)
    {

        var sut = new UpdateSettingsViewModel(CreateStore()) { IntervalHoursText = text };

        sut.IntervalError.Should().BeNull();
        sut.CanApply.Should().BeTrue();
    }

    private UpdateSettingsStore CreateStore()
        => new(Path.Combine(_directoryPath, "settings.json"));
}
