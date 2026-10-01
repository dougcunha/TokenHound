using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.App.Presentation;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies presets, the Apply pattern, and failure handling of <see cref="HudSizeSettingsViewModel"/> (TC-04..TC-06).
/// </summary>
public sealed class HudSizeSettingsViewModelTests
{
    /// <summary>
    /// Verifies that the persisted size is the baseline and that presets and the slider drive the label (TC-04).
    /// </summary>
    [Fact]
    public void PresetsAndSlider_UpdatePercentAndLabel()
    {

        var sut = Create(new HudSizeSettings { Percent = 100 }, out _, out _);

        sut.Percent.Should().Be(100);
        sut.Label.Should().Be("100%");
        sut.CanApply.Should().BeFalse();

        sut.SetPreset(HudSizeSettingsViewModel.SMALL_PERCENT);

        sut.Percent.Should().Be(75);
        sut.CanApply.Should().BeTrue();

        sut.PresetCommand.Execute("125");
        sut.Percent.Should().Be(125);

        sut.Percent = 60;

        sut.Label.Should().Be("60%");
    }

    /// <summary>
    /// Verifies that returning to the persisted value turns Apply off again (TC-04).
    /// </summary>
    [Fact]
    public void CanApply_TracksDifferenceFromPersistedSize()
    {

        var sut = Create(new HudSizeSettings { Percent = 70 }, out _, out _);

        sut.Percent = 90;
        sut.ApplyCommand.CanExecute(null).Should().BeTrue();

        sut.Percent = 70;

        sut.CanApply.Should().BeFalse();
        sut.ApplyCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Apply saves the size, updates the live scale, and becomes a no-op afterwards (TC-05).
    /// </summary>
    [Fact]
    public void Apply_SavesAndUpdatesLiveScale()
    {

        var sut = Create(new HudSizeSettings(), out var scale, out var saved);
        sut.Percent = 70;

        sut.Apply();

        saved.Should().ContainSingle().Which.Percent.Should().Be(70);
        scale.Percent.Should().Be(70);
        sut.IsApplied.Should().BeTrue();
        sut.CanApply.Should().BeFalse();

        sut.Apply();

        saved.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that a failed save keeps the live scale and the baseline and reports an error (TC-06).
    /// </summary>
    [Fact]
    public void Apply_WhenSaveFails_KeepsScaleAndSetsError()
    {

        var scale = new HudScale();
        var sut = new HudSizeSettingsViewModel(new HudSizeSettings(), static _ => false, scale);
        sut.Percent = 60;

        sut.Apply();

        scale.Percent.Should().Be(100);
        sut.ApplyError.Should().Be(HudSizeSettingsViewModel.APPLY_ERROR);
        sut.IsApplied.Should().BeFalse();
        sut.IsDirty.Should().BeTrue();

        sut.Percent = 70;

        sut.ApplyError.Should().BeNull();
    }

    private static HudSizeSettingsViewModel Create(
        HudSizeSettings settings,
        out HudScale scale,
        out List<HudSizeSettings> saved
    )
    {

        var savedSettings = new List<HudSizeSettings>();
        saved = savedSettings;
        scale = new HudScale();

        return new HudSizeSettingsViewModel(
            settings,
            settings1 =>
            {

                savedSettings.Add(settings1);

                return true;
            },
            scale
        );
    }
}
