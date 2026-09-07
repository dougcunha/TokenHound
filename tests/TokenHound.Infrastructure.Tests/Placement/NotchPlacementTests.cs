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
}
