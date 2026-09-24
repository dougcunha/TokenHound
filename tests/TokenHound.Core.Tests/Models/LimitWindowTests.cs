using AwesomeAssertions;
using TokenHound.Core.Models;

namespace TokenHound.Core.Tests.Models;

/// <summary>
/// Verifies the reported and absent measurements on a limit window.
/// </summary>
public sealed class LimitWindowTests
{
    /// <summary>
    /// Verifies that a remaining count does not imply a used fraction.
    /// </summary>
    [Fact]
    public void RemainingOnly_HasNoUsedFraction()
    {

        var window = new LimitWindow { Name = "Hourly", RemainingUnits = 500 };

        window.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an API supplied fraction needs no unit denominator.
    /// </summary>
    [Fact]
    public void ExplicitFraction_WithoutTotal_IsPreserved()
    {

        var window = new LimitWindow { Name = "Session", UsedFraction = 0.75 };

        window.TotalUnits.Should().BeNull();
        window.RemainingUnits.Should().BeNull();
        window.UsedFraction.Should().Be(0.75);
    }

    /// <summary>
    /// Verifies that explicit and absent fractions retain their meaning with a total.
    /// </summary>
    [Fact]
    public void WithTotal_PreservesExplicitAndAbsentFractions()
    {

        var reported = new LimitWindow { Name = "Daily", TotalUnits = 100, UsedFraction = 0.8 };
        var absent = new LimitWindow { Name = "Monthly", TotalUnits = 100 };

        reported.UsedFraction.Should().Be(0.8);
        absent.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a fractional remaining value is independent of integral units.
    /// </summary>
    [Fact]
    public void FractionalRemainingValue_IsPreserved()
    {

        var window = new LimitWindow
        {
            Name = "Monthly Premium Interactions",
            RemainingUnits = 10,
            RemainingValue = 10.5,
            TotalUnits = 100,
            UsedFraction = 0.895
        };

        window.RemainingValue.Should().Be(10.5);
    }

    /// <summary>
    /// Verifies that record copies preserve independent window values.
    /// </summary>
    [Fact]
    public void WithExpression_PreservesOriginal()
    {

        var reset = DateTimeOffset.UtcNow.AddHours(1);
        var original = new LimitWindow
        {
            Name = "Tokens",
            RemainingUnits = 50,
            TotalUnits = 100,
            UsedFraction = 0.5,
            ResetTimeUtc = reset,
            Period = TimeSpan.FromHours(5)
        };

        var modified = original with { RemainingUnits = 40, UsedFraction = 0.6 };

        original.RemainingUnits.Should().Be(50);
        original.UsedFraction.Should().Be(0.5);
        modified.RemainingUnits.Should().Be(40);
        modified.UsedFraction.Should().Be(0.6);
        modified.ResetTimeUtc.Should().Be(reset);
        modified.Period.Should().Be(original.Period);
    }
}
