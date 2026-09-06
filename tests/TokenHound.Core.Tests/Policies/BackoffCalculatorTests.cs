using AwesomeAssertions;
using System;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies exponential backoff progression, full jitter distribution, and boundary caps in <see cref="BackoffCalculator"/>.
/// </summary>
public sealed class BackoffCalculatorTests
{
    /// <summary>
    /// Verifies that zero or negative consecutive failures produce zero backoff duration.
    /// </summary>
    [Fact]
    public void CalculateBackoff_WhenFailuresZeroOrNegative_ReturnsZero()
    {
        var zeroFailuresInterval = BackoffCalculator.CalculateExponentialInterval(0);
        var zeroFailuresBackoff = BackoffCalculator.CalculateBackoff(0);
        var negativeFailuresInterval = BackoffCalculator.CalculateExponentialInterval(-3);
        var negativeFailuresBackoff = BackoffCalculator.CalculateBackoff(-3);

        zeroFailuresInterval.Should().Be(TimeSpan.Zero);
        zeroFailuresBackoff.Should().Be(TimeSpan.Zero);
        negativeFailuresInterval.Should().Be(TimeSpan.Zero);
        negativeFailuresBackoff.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that 1 consecutive failure calculates a 60-second base interval floor.
    /// </summary>
    [Fact]
    public void CalculateBackoff_WhenOneFailure_UsesSixtySecondFloor()
    {
        var expectedInterval = BackoffCalculator.CalculateExponentialInterval(1);
        var minJitter = BackoffCalculator.CalculateBackoff(1, 0.0);
        var halfJitter = BackoffCalculator.CalculateBackoff(1, 0.5);
        var maxJitter = BackoffCalculator.CalculateBackoff(1, 1.0);

        expectedInterval.Should().Be(TimeSpan.FromSeconds(60));
        minJitter.Should().Be(TimeSpan.Zero);
        halfJitter.Should().Be(TimeSpan.FromSeconds(30));
        maxJitter.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that 5 consecutive failures calculate a 960-second base interval (60 * 2^4).
    /// </summary>
    [Fact]
    public void CalculateBackoff_WhenFiveFailures_CalculatesNineHundredSixtySeconds()
    {
        var expectedInterval = BackoffCalculator.CalculateExponentialInterval(5);
        var minJitter = BackoffCalculator.CalculateBackoff(5, 0.0);
        var halfJitter = BackoffCalculator.CalculateBackoff(5, 0.5);
        var maxJitter = BackoffCalculator.CalculateBackoff(5, 1.0);

        expectedInterval.Should().Be(TimeSpan.FromSeconds(960));
        minJitter.Should().Be(TimeSpan.Zero);
        halfJitter.Should().Be(TimeSpan.FromSeconds(480));
        maxJitter.Should().Be(TimeSpan.FromSeconds(960));
    }

    /// <summary>
    /// Verifies that exponential intervals increase monotonically and cap at the 3600-second ceiling.
    /// </summary>
    [Fact]
    public void CalculateExponentialInterval_IncreasesMonotonicallyAndCapsAtCeiling()
    {
        var interval1 = BackoffCalculator.CalculateExponentialInterval(1);
        var interval2 = BackoffCalculator.CalculateExponentialInterval(2);
        var interval3 = BackoffCalculator.CalculateExponentialInterval(3);
        var interval4 = BackoffCalculator.CalculateExponentialInterval(4);
        var interval5 = BackoffCalculator.CalculateExponentialInterval(5);
        var interval6 = BackoffCalculator.CalculateExponentialInterval(6);
        var interval7 = BackoffCalculator.CalculateExponentialInterval(7);
        var interval20 = BackoffCalculator.CalculateExponentialInterval(20);

        interval1.Should().Be(TimeSpan.FromSeconds(60));
        interval2.Should().Be(TimeSpan.FromSeconds(120));
        interval3.Should().Be(TimeSpan.FromSeconds(240));
        interval4.Should().Be(TimeSpan.FromSeconds(480));
        interval5.Should().Be(TimeSpan.FromSeconds(960));
        interval6.Should().Be(TimeSpan.FromSeconds(1920));
        interval7.Should().Be(TimeSpan.FromSeconds(3600));
        interval20.Should().Be(TimeSpan.FromSeconds(3600));
    }

    /// <summary>
    /// Verifies that random jitter produces values strictly bounded between zero and the calculated exponential ceiling.
    /// </summary>
    [Fact]
    public void CalculateBackoff_WithRandomJitter_StaysWithinBoundsAndExhibitsVariance()
    {
        var maxInterval = BackoffCalculator.CalculateExponentialInterval(5);
        var random = new Random(12345);
        var minObserved = TimeSpan.MaxValue;
        var maxObserved = TimeSpan.MinValue;

        for (var i = 0; i < 50; i++)
        {
            var sample = BackoffCalculator.CalculateBackoff(5, random);

            sample.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
            sample.Should().BeLessThanOrEqualTo(maxInterval);

            if (sample < minObserved)
                minObserved = sample;

            if (sample > maxObserved)
                maxObserved = sample;
        }

        (maxObserved > minObserved).Should().BeTrue();
    }

    /// <summary>
    /// Verifies exposed constants and properties on <see cref="BackoffCalculator"/>.
    /// </summary>
    [Fact]
    public void BackoffCalculator_Properties_MatchExpectedLimits()
    {
        BackoffCalculator.Floor.Should().Be(TimeSpan.FromSeconds(60));
        BackoffCalculator.Ceiling.Should().Be(TimeSpan.FromSeconds(3600));
        BackoffCalculator.MINIMUM_FLOOR.Should().Be(TimeSpan.FromSeconds(60));
        BackoffCalculator.MAXIMUM_CEILING.Should().Be(TimeSpan.FromSeconds(3600));
    }
}
