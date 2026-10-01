using AwesomeAssertions;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies the bounds, clamping, and default resolution of <see cref="HudSizeSettings"/> (TC-01).
/// </summary>
public sealed class HudSizeSettingsTests
{
    /// <summary>
    /// Verifies that persisted values resolve to the nearest accepted size inside the accepted range.
    /// </summary>
    /// <param name="persisted">The persisted percentage, or <see langword="null"/> when absent.</param>
    /// <param name="expected">The expected effective percentage.</param>
    [Theory]
    [InlineData(null, 100)]
    [InlineData(0, 50)]
    [InlineData(-30, 50)]
    [InlineData(49, 50)]
    [InlineData(50, 50)]
    [InlineData(77, 75)]
    [InlineData(75, 75)]
    [InlineData(73, 75)]
    [InlineData(100, 100)]
    [InlineData(125, 125)]
    [InlineData(150, 150)]
    [InlineData(151, 150)]
    [InlineData(1000, 150)]
    public void ResolvedPercent_ClampsAndRoundsToStep(int? persisted, int expected)
    {

        var settings = new HudSizeSettings { Percent = persisted };

        settings.ResolvedPercent.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that the scale factor is the resolved percentage over one hundred, and exactly one at the default.
    /// </summary>
    [Fact]
    public void Factor_IsResolvedPercentOverOneHundred()
    {

        new HudSizeSettings().Factor.Should().Be(1.0);
        new HudSizeSettings { Percent = 70 }.Factor.Should().Be(0.7);
        new HudSizeSettings { Percent = 400 }.Factor.Should().Be(1.5);
    }
}
