using AwesomeAssertions;
using TokenHound.App.UI.Placement;
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
}
