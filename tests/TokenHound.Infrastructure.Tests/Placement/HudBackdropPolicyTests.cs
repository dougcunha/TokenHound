using AwesomeAssertions;
using TokenHound.App.UI.Placement;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>Verifies that every disabling input yields the solid fill (TC-01).</summary>
public sealed class HudBackdropPolicyTests
{

    private static readonly HudBackdropInputs AVAILABLE = new()
    {
        IsEnabled = true,
        OsBuild = HudBackdropPolicy.MINIMUM_BUILD,
        TransparencyEnabled = true,
        EnergySaverActive = false,
        HighContrast = false
    };

    /// <summary>Verifies that the material is chosen only when every input allows it.</summary>
    [Fact]
    public void Resolve_WhenEveryInputAllows_ReturnsMaterial()
    {

        HudBackdropPolicy.Resolve(AVAILABLE).Should().Be(HudBackdropMode.Material);
        HudBackdropPolicy.Reason(AVAILABLE).Should().Be(HudBackdropPolicy.AVAILABLE);
    }

    /// <summary>Verifies that each disabling input alone selects the solid fill and is named as the reason.</summary>
    [Theory]
    [InlineData(nameof(HudBackdropInputs.IsEnabled))]
    [InlineData(nameof(HudBackdropInputs.OsBuild))]
    [InlineData(nameof(HudBackdropInputs.TransparencyEnabled))]
    [InlineData(nameof(HudBackdropInputs.EnergySaverActive))]
    [InlineData(nameof(HudBackdropInputs.HighContrast))]
    public void Resolve_WhenOneInputDisables_ReturnsSolidWithThatReason(string input)
    {

        var inputs = Disable(input);

        HudBackdropPolicy.Resolve(inputs).Should().Be(HudBackdropMode.Solid);
        HudBackdropPolicy.Reason(inputs).Should().Be(input);
    }

    /// <summary>Verifies that Windows 10 and pre-22000 builds fall back to the solid fill.</summary>
    [Theory]
    [InlineData(19045, false)]
    [InlineData(HudBackdropPolicy.MINIMUM_BUILD - 1, false)]
    [InlineData(HudBackdropPolicy.MINIMUM_BUILD, true)]
    [InlineData(26200, true)]
    public void Resolve_UsesTheMinimumBuild(int build, bool expectsMaterial)
        => (HudBackdropPolicy.Resolve(AVAILABLE with { OsBuild = build }) is HudBackdropMode.Material).Should().Be(expectsMaterial);

    /// <summary>Verifies that every disabling input together still reports the user setting first.</summary>
    [Fact]
    public void Reason_WhenEverythingDisables_ReportsTheSettingFirst()
    {

        var inputs = new HudBackdropInputs
        {
            IsEnabled = false,
            OsBuild = 0,
            TransparencyEnabled = false,
            EnergySaverActive = true,
            HighContrast = true
        };

        HudBackdropPolicy.Reason(inputs).Should().Be(nameof(HudBackdropInputs.IsEnabled));
    }

    private static HudBackdropInputs Disable(string input)
        => input switch
        {
            nameof(HudBackdropInputs.IsEnabled) => AVAILABLE with { IsEnabled = false },
            nameof(HudBackdropInputs.OsBuild) => AVAILABLE with { OsBuild = HudBackdropPolicy.MINIMUM_BUILD - 1 },
            nameof(HudBackdropInputs.TransparencyEnabled) => AVAILABLE with { TransparencyEnabled = false },
            nameof(HudBackdropInputs.EnergySaverActive) => AVAILABLE with { EnergySaverActive = true },
            _ => AVAILABLE with { HighContrast = true }
        };
}
