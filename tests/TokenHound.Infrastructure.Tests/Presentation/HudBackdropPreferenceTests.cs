using AwesomeAssertions;
using System.Collections.Generic;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Tests.Presentation;

/// <summary>Verifies the Settings notice that explains why Windows keeps the HUD on the solid background.</summary>
public sealed class HudBackdropPreferenceTests
{

    /// <summary>Verifies that each system reason maps to its notice and Windows settings page.</summary>
    [Theory]
    [InlineData(nameof(HudBackdropInputs.TransparencyEnabled), HudBackdropPreference.TRANSPARENCY_NOTICE, "ms-settings:colors")]
    [InlineData(nameof(HudBackdropInputs.EnergySaverActive), HudBackdropPreference.ENERGY_SAVER_NOTICE, "ms-settings:batterysaver")]
    [InlineData(nameof(HudBackdropInputs.HighContrast), HudBackdropPreference.HIGH_CONTRAST_NOTICE, "ms-settings:easeofaccess-highcontrast")]
    [InlineData(nameof(HudBackdropInputs.OsBuild), HudBackdropPreference.OS_BUILD_NOTICE, null)]
    public void Notice_WhenEnabledAndSystemBlocksMaterial_ExplainsReason(string reason, string notice, string? uri)
    {

        var preference = new HudBackdropPreference { Unavailability = reason };

        preference.Notice.Should().Be(notice);
        preference.SystemSettingsUri.Should().Be(uri);
    }

    /// <summary>Verifies that no notice shows while the material is available.</summary>
    [Fact]
    public void Notice_WhenMaterialIsAvailable_IsNull()
    {

        var preference = new HudBackdropPreference();

        preference.Notice.Should().BeNull();
        preference.SystemSettingsUri.Should().BeNull();
    }

    /// <summary>Verifies that turning the setting off hides the notice, because the solid background is then expected.</summary>
    [Fact]
    public void Notice_WhenSettingIsOff_IsNull()
    {

        var preference = new HudBackdropPreference { Unavailability = nameof(HudBackdropInputs.TransparencyEnabled) };

        preference.IsEnabled = false;

        preference.Notice.Should().BeNull();
        preference.SystemSettingsUri.Should().BeNull();
    }

    /// <summary>Verifies that a live change of either input raises the notice change for the bound card.</summary>
    [Fact]
    public void Notice_WhenInputsChange_RaisesNoticeChanged()
    {

        var changed = new List<string?>();
        var preference = new HudBackdropPreference();
        preference.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        preference.Unavailability = nameof(HudBackdropInputs.TransparencyEnabled);
        preference.IsEnabled = false;

        changed.Should().ContainInOrder(nameof(HudBackdropPreference.Notice), nameof(HudBackdropPreference.Notice));
        changed.Should().Contain(nameof(HudBackdropPreference.SystemSettingsUri));
    }

    /// <summary>Verifies the default tint opacity and its alpha mapping, which keeps 7 % at the approved 0xED tint.</summary>
    [Theory]
    [InlineData(0, 0xFF)]
    [InlineData(7, 0xED)]
    [InlineData(30, 0xB2)]
    [InlineData(-5, 0xFF)]
    [InlineData(50, 0x80)]
    [InlineData(120, 0x80)]
    public void TintAlpha_MapsTransparencyToFillAlpha(int transparency, int alpha)
        => HudBackdropPreference.TintAlpha(transparency).Should().Be((byte)alpha);

    /// <summary>Verifies that a transparency change raises its notification for the HUD tint.</summary>
    [Fact]
    public void Transparency_WhenChanged_RaisesChange()
    {

        var changed = new List<string?>();
        var preference = new HudBackdropPreference();
        preference.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        preference.Transparency.Should().Be(HudBackdropSettings.DEFAULT_TRANSPARENCY);
        preference.Transparency = 12;

        changed.Should().Contain(nameof(HudBackdropPreference.Transparency));
    }

    /// <summary>Verifies that the live transparency is clamped like the stored value.</summary>
    [Fact]
    public void Transparency_WhenOutOfRange_IsClamped()
    {

        var preference = new HudBackdropPreference { Transparency = 90 };

        preference.Transparency.Should().Be(HudBackdropSettings.MAXIMUM_TRANSPARENCY);
    }

    /// <summary>Verifies that setting the same reason again raises nothing.</summary>
    [Fact]
    public void Unavailability_WhenUnchanged_RaisesNothing()
    {

        var preference = new HudBackdropPreference { Unavailability = nameof(HudBackdropInputs.HighContrast) };
        var raised = false;
        preference.PropertyChanged += (_, _) => raised = true;

        preference.Unavailability = nameof(HudBackdropInputs.HighContrast);

        raised.Should().BeFalse();
    }
}
