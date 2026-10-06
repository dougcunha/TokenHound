using AwesomeAssertions;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies placement mode resolution, migration, and fallback performed by <see cref="HudPositionSettings.ResolveMode"/>.
/// </summary>
public sealed class HudPositionSettingsTests
{
    [Fact]
    public void ResolveMode_WhenNothingIsStored_ReturnsTopCenter()
    {

        var (mode, warning) = new HudPositionSettings().ResolveMode();

        mode.Should().Be(HudDockMode.TopCenter);
        warning.Should().BeNull();
    }

    [Fact]
    public void ResolveMode_WhenOnlyCoordinatesAreStored_MigratesToFree()
    {

        var (mode, warning) = new HudPositionSettings { Left = 10, Top = 0 }.ResolveMode();

        mode.Should().Be(HudDockMode.Free);
        warning.Should().BeNull();
    }

    [Theory]
    [InlineData("TopLeft", HudDockMode.TopLeft)]
    [InlineData("topcenter", HudDockMode.TopCenter)]
    [InlineData("TOPRIGHT", HudDockMode.TopRight)]
    [InlineData("LeftEdge", HudDockMode.LeftEdge)]
    [InlineData("rightedge", HudDockMode.RightEdge)]
    public void ResolveMode_WhenModeIsKnown_ReturnsItInAnyCasing(string stored, HudDockMode expected)
    {

        var (mode, warning) = new HudPositionSettings { Mode = stored }.ResolveMode();

        mode.Should().Be(expected);
        warning.Should().BeNull();
    }

    [Fact]
    public void ResolveMode_WhenFreeHasCoordinates_ReturnsFree()
    {

        var (mode, warning) = new HudPositionSettings { Mode = "Free", Left = 500, Top = 300 }.ResolveMode();

        mode.Should().Be(HudDockMode.Free);
        warning.Should().BeNull();
    }

    [Theory]
    [InlineData("Diagonal")]
    [InlineData("3")]
    [InlineData("TopLeft, TopCenter")]
    [InlineData("")]
    public void ResolveMode_WhenModeIsUnknown_FallsBackToTopCenterWithWarning(string stored)
    {

        var (mode, warning) = new HudPositionSettings { Mode = stored, Left = 10, Top = 0 }.ResolveMode();

        mode.Should().Be(HudDockMode.TopCenter);
        warning.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveMode_WhenFreeHasNoCoordinates_FallsBackToTopCenterWithWarning()
    {

        var (mode, warning) = new HudPositionSettings { Mode = "Free" }.ResolveMode();

        mode.Should().Be(HudDockMode.TopCenter);
        warning.Should().NotBeNullOrWhiteSpace();
    }
}
