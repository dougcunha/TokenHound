using AwesomeAssertions;
using System;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>
/// Verifies how <see cref="NotchPlacement"/> slides a docked capsule along its edge.
/// </summary>
public sealed class NotchPlacementEdgeTests
{
    private const double SNAP = 16;

    private static readonly ScreenBounds WORK_AREA = new()
    {
        Left = -1920,
        Top = 40,
        Width = 1920,
        Height = 1000
    };

    [Theory]
    [InlineData(HudDockMode.TopCenter, 0.25, -1515, 40)]
    [InlineData(HudDockMode.TopLeft, 1, -300, 40)]
    [InlineData(HudDockMode.LeftEdge, 0, -1920, 40)]
    [InlineData(HudDockMode.RightEdge, 1, -300, 740)]
    public void Dock_WithEdgePosition_SlidesAlongTheEdge(HudDockMode mode, double position, double expectedLeft, double expectedTop)
    {

        var (left, top) = NotchPlacement.Dock(
            mode,
            WORK_AREA,
            windowWidth: 300,
            windowHeight: 300,
            edgePosition: position
        );

        left.Should().Be(expectedLeft);
        top.Should().Be(expectedTop);
    }

    [Fact]
    public void Dock_WithEdgePositionOutOfRange_StaysInsideTheWorkArea()
    {

        var (left, _) = NotchPlacement.Dock(
            HudDockMode.TopCenter,
            WORK_AREA,
            windowWidth: 300,
            windowHeight: 60,
            edgePosition: 4
        );

        left.Should().Be(-300);
    }

    [Theory]
    [InlineData(-1000, 500, -1000, 40)]
    [InlineData(500, -200, -300, 40)]
    [InlineData(-1910, 40, -1920, 40)]
    [InlineData(-310, 40, -300, 40)]
    public void Slide_OnTopEdge_KeepsTheEdgeAndSnapsToTheEnds(double proposedLeft, double proposedTop, double expectedLeft, double expectedTop)
    {

        var (left, top) = NotchPlacement.Slide(
            HudDockMode.TopCenter,
            WORK_AREA,
            windowWidth: 300,
            windowHeight: 60,
            proposedLeft,
            proposedTop,
            SNAP
        );

        left.Should().Be(expectedLeft);
        top.Should().Be(expectedTop);
    }

    [Fact]
    public void Slide_OnRightEdge_KeepsTheEdgeAndMovesVertically()
    {

        var (left, top) = NotchPlacement.Slide(
            HudDockMode.RightEdge,
            WORK_AREA,
            windowWidth: 60,
            windowHeight: 300,
            left: -900,
            top: 300,
            SNAP
        );

        left.Should().Be(-60);
        top.Should().Be(300);
    }

    [Fact]
    public void EdgePosition_RoundTripsThroughDock()
    {

        var position = NotchPlacement.EdgePosition(
            HudDockMode.LeftEdge,
            WORK_AREA,
            windowWidth: 60,
            windowHeight: 300,
            left: -1920,
            top: 313
        );

        var (_, top) = NotchPlacement.Dock(
            HudDockMode.LeftEdge,
            WORK_AREA,
            windowWidth: 60,
            windowHeight: 300,
            edgePosition: position
        );

        position.Should().BeApproximately(0.39, 1e-9);
        top.Should().Be(313);
    }

    [Fact]
    public void EdgePosition_WhenWindowFillsTheEdge_ReturnsTheMiddle()
    {

        var position = NotchPlacement.EdgePosition(
            HudDockMode.TopCenter,
            WORK_AREA,
            windowWidth: 2000,
            windowHeight: 60,
            left: -1920,
            top: 40
        );

        position.Should().Be(0.5);
    }

    [Fact]
    public void Slide_InFreeMode_Throws()
    {

        var slide = static () => NotchPlacement.Slide(
            HudDockMode.Free,
            WORK_AREA,
            windowWidth: 300,
            windowHeight: 60,
            left: 0,
            top: 0,
            SNAP
        );

        slide.Should().Throw<ArgumentOutOfRangeException>();
    }
}
