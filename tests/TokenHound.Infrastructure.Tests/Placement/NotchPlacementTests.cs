using AwesomeAssertions;
using System;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>
/// Verifies default centering and visibility clamping performed by <see cref="NotchPlacement"/>.
/// </summary>
public sealed class NotchPlacementTests
{
    private static readonly ScreenBounds PRIMARY_SCREEN = new()
    {
        Left = 0,
        Top = 0,
        Width = 3440,
        Height = 1440
    };

    private static readonly ScreenBounds LEFT_SECONDARY = new()
    {
        Left = -1920,
        Top = 40,
        Width = 1920,
        Height = 1000
    };

    [Fact]
    public void CenterOnTopEdge_AlignsCapsuleWithWorkAreaTop()
    {

        var (left, top) = NotchPlacement.CenterOnTopEdge(PRIMARY_SCREEN, windowWidth: 240);

        left.Should().Be(1600);
        top.Should().Be(0);
    }

    [Fact]
    public void CenterOnTopEdge_HonorsWorkAreaOffset()
    {

        var workArea = new ScreenBounds
        {
            Left = -1920,
            Top = 40,
            Width = 1920,
            Height = 1000
        };

        var (left, top) = NotchPlacement.CenterOnTopEdge(workArea, windowWidth: 200);

        left.Should().Be(-1060);
        top.Should().Be(40);
    }

    [Fact]
    public void Clamp_WhenPositionIsVisible_KeepsItUnchanged()
    {

        var (left, top) = NotchPlacement.Clamp(
            PRIMARY_SCREEN,
            left: 800,
            top: 300,
            windowWidth: 240,
            windowHeight: 70
        );

        left.Should().Be(800);
        top.Should().Be(300);
    }

    [Fact]
    public void Clamp_WhenPositionIsOffScreen_PullsCapsuleBackIntoBounds()
    {

        var (left, top) = NotchPlacement.Clamp(
            PRIMARY_SCREEN,
            left: 9000,
            top: -500,
            windowWidth: 240,
            windowHeight: 70
        );

        left.Should().Be(3200);
        top.Should().Be(0);
    }

    [Fact]
    public void Clamp_WhenWindowIsLargerThanBounds_AnchorsToTopLeft()
    {

        var (left, top) = NotchPlacement.Clamp(
            PRIMARY_SCREEN,
            left: 500,
            top: 500,
            windowWidth: 5000,
            windowHeight: 2000
        );

        left.Should().Be(0);
        top.Should().Be(0);
    }

    [Fact]
    public void CenterOnTopEdge_WhenCapsuleShrinks_RecentersOnTheSamePoint()
    {

        var (wideLeft, _) = NotchPlacement.CenterOnTopEdge(PRIMARY_SCREEN, windowWidth: 240);
        var (narrowLeft, _) = NotchPlacement.CenterOnTopEdge(PRIMARY_SCREEN, windowWidth: 120);

        (wideLeft + 120).Should().Be(narrowLeft + 60);
    }

    [Fact]
    public void Clamp_WhenCapsuleGrowsNearRightEdge_PullsItBackIntoBounds()
    {

        var (left, top) = NotchPlacement.Clamp(
            PRIMARY_SCREEN,
            left: 3300,
            top: 10,
            windowWidth: 360,
            windowHeight: 105
        );

        left.Should().Be(3080);
        top.Should().Be(10);
    }

    [Fact]
    public void Clamp_WhenCapsuleShrinks_KeepsDraggedPositionUnchanged()
    {

        var (left, top) = NotchPlacement.Clamp(
            PRIMARY_SCREEN,
            left: 3300,
            top: 10,
            windowWidth: 120,
            windowHeight: 35
        );

        left.Should().Be(3300);
        top.Should().Be(10);
    }

    /// <summary>Recovers the saved position from a gap in a nonrectangular monitor arrangement.</summary>
    [Fact]
    public void Clamp_WhenSavedPositionIsInMonitorGap_UsesActualMonitorBounds()
    {

        var primary = new ScreenBounds { Left = 0, Top = 0, Width = 1280, Height = 720 };
        var (left, top) = NotchPlacement.Clamp(
            primary,
            left: 2242,
            top: 0,
            windowWidth: 240,
            windowHeight: 70
        );

        left.Should().Be(1040);
        top.Should().Be(0);
    }

    /// <summary>Keeps the HUD fully visible when its nearest monitor is above the primary display.</summary>
    [Fact]
    public void Clamp_WhenNearestMonitorIsAbovePrimary_RecoversIntoNegativeCoordinates()
    {

        var upper = new ScreenBounds { Left = 0, Top = -1440, Width = 3440, Height = 1392 };
        var (left, top) = NotchPlacement.Clamp(
            upper,
            left: 2242,
            top: 0,
            windowWidth: 240,
            windowHeight: 70
        );

        left.Should().Be(2242);
        top.Should().Be(-118);
    }

    [Theory]
    [InlineData(HudDockMode.TopLeft, -1920, 40)]
    [InlineData(HudDockMode.TopCenter, -1110, 40)]
    [InlineData(HudDockMode.TopRight, -300, 40)]
    public void Dock_TopModes_AreFlushWithTopEdgeOfNegativeOriginWorkArea(HudDockMode mode, double expectedLeft, double expectedTop)
    {

        var (left, top) = NotchPlacement.Dock(
            mode,
            LEFT_SECONDARY,
            windowWidth: 300,
            windowHeight: 60
        );

        left.Should().Be(expectedLeft);
        top.Should().Be(expectedTop);
    }

    [Theory]
    [InlineData(HudDockMode.LeftEdge, -1920, 390)]
    [InlineData(HudDockMode.RightEdge, -60, 390)]
    public void Dock_SideModes_AreFlushAndVerticallyCentered(HudDockMode mode, double expectedLeft, double expectedTop)
    {

        var (left, top) = NotchPlacement.Dock(
            mode,
            LEFT_SECONDARY,
            windowWidth: 60,
            windowHeight: 300
        );

        left.Should().Be(expectedLeft);
        top.Should().Be(expectedTop);
    }

    /// <summary>An odd remainder floors so the capsule never sits half a pixel off a whole-pixel grid.</summary>
    [Fact]
    public void Dock_TopCenter_WithOddRemainder_FloorsTheOffset()
    {

        var workArea = new ScreenBounds { Left = 0, Top = 0, Width = 1921, Height = 1080 };

        var (left, _) = NotchPlacement.Dock(
            HudDockMode.TopCenter,
            workArea,
            windowWidth: 200,
            windowHeight: 60
        );

        left.Should().Be(860);
    }

    [Fact]
    public void Dock_TopCenter_MatchesCenterOnTopEdgeForEvenRemainders()
    {

        var (left, top) = NotchPlacement.Dock(
            HudDockMode.TopCenter,
            PRIMARY_SCREEN,
            windowWidth: 240,
            windowHeight: 70
        );

        (left, top).Should().Be(NotchPlacement.CenterOnTopEdge(PRIMARY_SCREEN, windowWidth: 240));
    }

    [Theory]
    [InlineData(HudDockMode.TopRight)]
    [InlineData(HudDockMode.RightEdge)]
    [InlineData(HudDockMode.TopCenter)]
    public void Dock_WhenWindowIsLargerThanWorkArea_StartsAtWorkAreaOrigin(HudDockMode mode)
    {

        var small = new ScreenBounds { Left = 100, Top = 50, Width = 200, Height = 100 };

        var (left, top) = NotchPlacement.Dock(
            mode,
            small,
            windowWidth: 400,
            windowHeight: 300
        );

        left.Should().Be(100);
        top.Should().Be(50);
    }

    [Fact]
    public void Dock_WhenModeIsFree_Throws()
    {

        var act = () => NotchPlacement.Dock(
            HudDockMode.Free,
            PRIMARY_SCREEN,
            windowWidth: 240,
            windowHeight: 70
        );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
