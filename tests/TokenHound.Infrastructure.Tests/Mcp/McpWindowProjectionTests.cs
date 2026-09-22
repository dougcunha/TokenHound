using AwesomeAssertions;
using NSubstitute;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Mcp;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Mcp;

/// <summary>Verifies that MCP window projections keep used and remaining counts distinct.</summary>
public sealed class McpWindowProjectionTests
{
    private static readonly DateTimeOffset SAMPLE_TIME = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies a used-count window is exposed as used units and never as remaining units.</summary>
    [Fact]
    public async Task UsedCountWindow_IsProjectedAsUsedUnitsOnly()
    {

        var window = new LimitWindow
        {
            Name = "Requests Today",
            UsedUnits = 7,
            RemainingUnits = null,
            TotalUnits = null,
            UsedFraction = null
        };
        using var store = new UsageStore();
        await PublishAsync(store, window, TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store);

        var projected = reader.GetMetrics("gemini").Provider!.LimitWindows.Should().ContainSingle().Subject;

        projected.UsedUnits.Should().Be(7);
        projected.RemainingUnits.Should().BeNull();
        projected.UsedFraction.Should().BeNull();
        projected.TotalUnits.Should().BeNull();
        JsonSerializer.Serialize(projected).Should().Contain("\"usedUnits\":7");
    }

    /// <summary>Verifies quota windows keep their remaining counts and report no used count.</summary>
    [Fact]
    public async Task QuotaWindow_KeepsRemainingUnitsWithoutUsedUnits()
    {

        var window = new LimitWindow
        {
            Name = "five_hour",
            UsedFraction = 0.36,
            RemainingUnits = 64,
            TotalUnits = 100
        };
        using var store = new UsageStore();
        await PublishAsync(store, window, TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store);

        var projected = reader.GetMetrics("gemini").Provider!.LimitWindows.Should().ContainSingle().Subject;

        projected.RemainingUnits.Should().Be(64);
        projected.UsedUnits.Should().BeNull();
        projected.UsedFraction.Should().Be(0.36);
    }

    private static async Task PublishAsync(UsageStore store, LimitWindow window, CancellationToken cancellationToken)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("gemini");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new Snapshot
        {
            ProviderId = "gemini",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = SAMPLE_TIME,
            LimitWindows = [window]
        }));
        store.RegisterProvider(provider);
        await store.RefreshNowAsync(cancellationToken);
    }
}
