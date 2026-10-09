using AwesomeAssertions;
using System.Collections.Generic;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>Verifies that the translucent background toggle applies immediately and survives save failures (FR-04).</summary>
public sealed class HudBackdropSettingsViewModelTests
{

    /// <summary>Verifies that a missing preference starts the toggle on.</summary>
    [Fact]
    public void Constructor_WhenPreferenceIsMissing_StartsEnabled()
    {

        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings(), static _ => true, new HudBackdropPreference());

        model.IsEnabled.Should().BeTrue();
    }

    /// <summary>Verifies that toggling saves the value and updates the live preference without an Apply step.</summary>
    [Fact]
    public void IsEnabled_WhenToggled_SavesAndUpdatesLivePreference()
    {

        var saved = new List<HudBackdropSettings>();
        var preference = new HudBackdropPreference();
        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings(), settings => Save(saved, settings), preference);

        model.IsEnabled = false;

        saved.Should().ContainSingle().Which.Enabled.Should().BeFalse();
        preference.IsEnabled.Should().BeFalse();
        model.ApplyError.Should().BeNull();
    }

    /// <summary>Verifies that a failed save keeps the previous value and the live preference, and reports an error.</summary>
    [Fact]
    public void IsEnabled_WhenSaveFails_KeepsPreviousValueAndReportsError()
    {

        var preference = new HudBackdropPreference();
        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings(), static _ => false, preference);

        model.IsEnabled = false;

        model.IsEnabled.Should().BeTrue();
        preference.IsEnabled.Should().BeTrue();
        model.ApplyError.Should().Be(HudBackdropSettingsViewModel.APPLY_ERROR);
    }

    /// <summary>Verifies that the slider starts at the stored transparency, clamped to the supported range.</summary>
    [Fact]
    public void Constructor_ReadsClampedTransparency()
    {

        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings { Transparency = 90 }, static _ => true, new HudBackdropPreference());

        model.Transparency.Should().Be(HudBackdropSettings.MAXIMUM_TRANSPARENCY);
        model.Minimum.Should().Be(HudBackdropSettings.MINIMUM_TRANSPARENCY);
        model.Maximum.Should().Be(HudBackdropSettings.MAXIMUM_TRANSPARENCY);
    }

    /// <summary>Verifies that moving the slider saves it with the toggle and updates the live HUD tint.</summary>
    [Fact]
    public void Transparency_WhenChanged_SavesWithToggleAndUpdatesLivePreference()
    {

        var saved = new List<HudBackdropSettings>();
        var preference = new HudBackdropPreference();
        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings(), settings => Save(saved, settings), preference);

        model.Transparency = 20;

        saved.Should().ContainSingle().Which.Should().Be(new HudBackdropSettings { Enabled = true, Transparency = 20 });
        preference.Transparency.Should().Be(20);
    }

    /// <summary>Verifies that toggling the background keeps the chosen transparency in the saved section.</summary>
    [Fact]
    public void IsEnabled_WhenToggled_KeepsTransparency()
    {

        var saved = new List<HudBackdropSettings>();
        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings { Transparency = 15 }, settings => Save(saved, settings), new HudBackdropPreference());

        model.IsEnabled = false;

        saved.Should().ContainSingle().Which.Should().Be(new HudBackdropSettings { Enabled = false, Transparency = 15 });
    }

    /// <summary>Verifies that a failed save keeps the previous transparency and the live tint.</summary>
    [Fact]
    public void Transparency_WhenSaveFails_KeepsPreviousValue()
    {

        var preference = new HudBackdropPreference();
        var model = new HudBackdropSettingsViewModel(new HudBackdropSettings(), static _ => false, preference);

        model.Transparency = 25;

        model.Transparency.Should().Be(HudBackdropSettings.DEFAULT_TRANSPARENCY);
        preference.Transparency.Should().Be(HudBackdropSettings.DEFAULT_TRANSPARENCY);
        model.ApplyError.Should().Be(HudBackdropSettingsViewModel.APPLY_ERROR);
    }

    /// <summary>Verifies that the notice button opens the Windows settings page for the current reason.</summary>
    [Fact]
    public void OpenSystemSettingsCommand_WhenTransparencyIsOff_OpensColorsPage()
    {

        var opened = new List<string>();
        var preference = new HudBackdropPreference { Unavailability = nameof(HudBackdropInputs.TransparencyEnabled) };
        var model = new HudBackdropSettingsViewModel(
            new HudBackdropSettings(),
            static _ => true,
            preference,
            opened.Add
        );

        model.OpenSystemSettingsCommand.Execute(null);

        model.Preference.Should().BeSameAs(preference);
        opened.Should().Equal("ms-settings:colors");
    }

    /// <summary>Verifies that the command does nothing when no Windows settings page applies.</summary>
    [Fact]
    public void OpenSystemSettingsCommand_WhenNoPageApplies_OpensNothing()
    {

        var opened = new List<string>();
        var preference = new HudBackdropPreference { Unavailability = nameof(HudBackdropInputs.OsBuild) };
        var model = new HudBackdropSettingsViewModel(
            new HudBackdropSettings(),
            static _ => true,
            preference,
            opened.Add
        );

        model.OpenSystemSettingsCommand.Execute(null);

        opened.Should().BeEmpty();
    }

    private static bool Save(List<HudBackdropSettings> saved, HudBackdropSettings settings)
    {

        saved.Add(settings);

        return true;
    }
}
