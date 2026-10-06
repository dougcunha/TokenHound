using AwesomeAssertions;
using System;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>
/// Verifies preferred-display matching and primary fallback performed by <see cref="DisplayResolver"/>.
/// </summary>
public sealed class DisplayResolverTests
{
    private const string DELL_EDID = "10AC:A1F2";

    private static readonly DisplayInfo LAPTOP = CreateDisplay(
        number: 1,
        devicePath: @"\\?\DISPLAY#BOE0A1B#4&1a2b#{e6f07b5f}",
        edidKey: "09E5:0A1B",
        isPrimary: true,
        left: 0
    );

    private static readonly DisplayInfo DELL_LEFT = CreateDisplay(
        number: 2,
        devicePath: @"\\?\DISPLAY#DELA1F2#5&2b3c&0&UID4352#{e6f07b5f}",
        edidKey: DELL_EDID,
        isPrimary: false,
        left: -2560
    );

    private static readonly DisplayInfo DELL_RIGHT = CreateDisplay(
        number: 3,
        devicePath: @"\\?\DISPLAY#DELA1F2#5&2b3c&0&UID4353#{e6f07b5f}",
        edidKey: DELL_EDID,
        isPrimary: false,
        left: 1920
    );

    [Fact]
    public void Resolve_WhenPreferenceIsNull_ReturnsPrimary()
    {

        var resolution = DisplayResolver.Resolve(null, [DELL_LEFT, LAPTOP]);

        resolution.Target.Should().Be(LAPTOP);
        resolution.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenDevicePathMatches_ReturnsThatDisplay()
    {

        var preference = DisplayResolver.ToPreference(DELL_RIGHT);

        var resolution = DisplayResolver.Resolve(preference, [LAPTOP, DELL_LEFT, DELL_RIGHT]);

        resolution.Target.Should().Be(DELL_RIGHT);
        resolution.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenDevicePathChangedButEdidIsUnique_ReturnsEdidMatch()
    {

        var preference = new HudDisplayPreference { DevicePath = @"\\?\DISPLAY#DELA1F2#old-port", EdidKey = DELL_EDID };

        var resolution = DisplayResolver.Resolve(preference, [LAPTOP, DELL_LEFT]);

        resolution.Target.Should().Be(DELL_LEFT);
        resolution.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenEdidMatchIsAmbiguous_FallsBackToPrimary()
    {

        var preference = new HudDisplayPreference { DevicePath = @"\\?\DISPLAY#DELA1F2#old-port", EdidKey = DELL_EDID };

        var resolution = DisplayResolver.Resolve(preference, [LAPTOP, DELL_LEFT, DELL_RIGHT]);

        resolution.Target.Should().Be(LAPTOP);
        resolution.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void Resolve_WhenPreferredDisplayIsDisconnected_FallsBackToPrimary()
    {

        var preference = DisplayResolver.ToPreference(DELL_RIGHT);

        var resolution = DisplayResolver.Resolve(preference, [LAPTOP]);

        resolution.Target.Should().Be(LAPTOP);
        resolution.IsFallback.Should().BeTrue();
    }

    [Fact]
    public void Resolve_WhenNoDisplayIsFlaggedPrimary_UsesFirstDisplay()
    {

        var resolution = DisplayResolver.Resolve(null, [DELL_LEFT, DELL_RIGHT]);

        resolution.Target.Should().Be(DELL_LEFT);
    }

    [Fact]
    public void Resolve_WhenPreferenceHasNoIdentityKeys_FollowsPrimaryWithoutFallback()
    {

        var resolution = DisplayResolver.Resolve(new HudDisplayPreference { Name = "Old" }, [DELL_LEFT, LAPTOP]);

        resolution.Target.Should().Be(LAPTOP);
        resolution.IsFallback.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenNoDisplays_Throws()
    {

        var act = () => DisplayResolver.Resolve(null, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FindHosting_ReturnsDisplayContainingOrNearestToPoint()
    {

        DisplayInfo[] displays = [LAPTOP, DELL_LEFT, DELL_RIGHT];

        DisplayResolver.FindHosting(displays, -1000, 500).Should().Be(DELL_LEFT);
        DisplayResolver.FindHosting(displays, -100, 500).Should().Be(LAPTOP);
        DisplayResolver.FindHosting(displays, 960, 500).Should().Be(LAPTOP);
        DisplayResolver.FindHosting(displays, 960, 1075).Should().Be(LAPTOP);
        DisplayResolver.FindHosting(displays, 9000, 0).Should().Be(DELL_RIGHT);
    }

    private static DisplayInfo CreateDisplay(
        int number,
        string devicePath,
        string edidKey,
        bool isPrimary,
        double left
    )
        => new()
        {
            DevicePath = devicePath,
            EdidKey = edidKey,
            Name = $"Display {number}",
            Number = number,
            IsPrimary = isPrimary,
            WorkArea = new ScreenBounds { Left = left, Top = 0, Width = 1920, Height = 1040 },
            Width = 1920,
            Height = 1080
        };
}
