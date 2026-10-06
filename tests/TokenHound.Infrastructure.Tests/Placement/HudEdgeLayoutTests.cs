using AwesomeAssertions;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>
/// Verifies the docked edge and orientation reported by <see cref="HudEdgeLayout"/>.
/// </summary>
public sealed class HudEdgeLayoutTests
{
    [Theory]
    [InlineData(HudDockMode.TopLeft, HudEdge.Top, false)]
    [InlineData(HudDockMode.TopCenter, HudEdge.Top, false)]
    [InlineData(HudDockMode.TopRight, HudEdge.Top, false)]
    [InlineData(HudDockMode.LeftEdge, HudEdge.Left, true)]
    [InlineData(HudDockMode.RightEdge, HudEdge.Right, true)]
    [InlineData(HudDockMode.Free, HudEdge.None, false)]
    public void For_MapsEachModeToItsEdgeAndOrientation(HudDockMode mode, HudEdge expectedEdge, bool expectedVertical)
    {

        var (edge, isVertical) = HudEdgeLayout.For(mode);

        edge.Should().Be(expectedEdge);
        isVertical.Should().Be(expectedVertical);
    }
}
