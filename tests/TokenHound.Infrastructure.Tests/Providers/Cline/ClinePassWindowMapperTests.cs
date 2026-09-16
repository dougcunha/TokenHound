using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies the Cline Pass window mapping: coverage rules, fraction math, and unpublished windows.
/// </summary>
public sealed class ClinePassWindowMapperTests
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 16, 20, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that missing caps produce no windows.</summary>
    [Fact]
    public void Map_WithoutCaps_ReturnsEmpty()
    {

        var windows = ClinePassWindowMapper.Map(null, [], NOW);

        windows.Should().BeEmpty();
    }

    /// <summary>Verifies that fully covered windows report the same-unit cost fraction.</summary>
    [Fact]
    public void Map_WithCompleteCoverage_ReportsFractions()
    {

        var caps = new ClineInferenceCapThreshold
        {
            Last5HoursCost = 1000.0,
            Last7DaysCost = 2000.0,
            Last30DaysCost = 4000.0
        };
        var transactions = new List<ClineUsageTransaction>
        {
            new()
            {
                CreatedAt = NOW.AddHours(-1),
                CostUsd = 250.0
            },
            new()
            {
                CreatedAt = NOW.AddDays(-10),
                CostUsd = 500.0
            },
            new()
            {
                CreatedAt = NOW.AddDays(-40),
                CostUsd = 750.0
            }
        };

        var windows = ClinePassWindowMapper.Map(caps, transactions, NOW);

        windows.Should().HaveCount(3);
        windows[0].Name.Should().Be("Cline Pass (5h)");
        windows[0].Period.Should().Be(TimeSpan.FromHours(5));
        windows[0].GroupName.Should().Be("Cline Pass");
        windows[0].UsedFraction.Should().BeApproximately(0.25, 1e-9);
        windows[0].TotalUnits.Should().Be(1000L);
        windows[0].RemainingUnits.Should().Be(750L);
        windows[1].Name.Should().Be("Cline Pass (7d)");
        windows[1].UsedFraction.Should().BeApproximately(0.125, 1e-9);
        windows[2].Name.Should().Be("Cline Pass (30d)");
        windows[2].UsedFraction.Should().BeApproximately(0.1875, 1e-9);
    }

    /// <summary>Verifies that unprovable coverage skips the window instead of inventing a fraction.</summary>
    [Fact]
    public void Map_WithTruncatedPage_SkipsWindow()
    {

        var caps = new ClineInferenceCapThreshold { Last30DaysCost = 4000.0 };
        var transactions = new List<ClineUsageTransaction>
        {
            new()
            {
                CreatedAt = NOW.AddHours(-1),
                CostUsd = 250.0
            }
        };

        var windows = ClinePassWindowMapper.Map(caps, transactions, NOW);

        windows.Should().BeEmpty();
    }

    /// <summary>Verifies that transactions without timestamps invalidate the window.</summary>
    [Fact]
    public void Map_WithDatelessTransactions_SkipsWindow()
    {

        var caps = new ClineInferenceCapThreshold { Last5HoursCost = 1000.0 };
        var transactions = new List<ClineUsageTransaction>
        {
            new()
            {
                CreatedAt = null,
                CostUsd = 900.0
            }
        };

        var windows = ClinePassWindowMapper.Map(caps, transactions, NOW);

        windows.Should().BeEmpty();
    }
}