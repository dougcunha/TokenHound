using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Mcp;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Mcp;

/// <summary>Verifies safe, read-only projections of current provider metrics.</summary>
public sealed class McpMetricsReaderTests
{
    private static readonly DateTimeOffset SAMPLE_TIME = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies list filtering and precise states for unavailable provider IDs.</summary>
    [Fact]
    public async Task ListAndLookup_FilterUnavailableProviders()
    {

        using var store = new UsageStore();
        Register(store, CreateSnapshot("zeta"));
        Register(store, CreateSnapshot("gamma"));
        Register(store, CreateSnapshot("alpha"));
        Register(store, CreateSnapshot("mock"));
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        store.SetProviderEnabled("zeta", false);
        Register(store, CreateSnapshot("pending"));
        var reader = new McpMetricsReader(store, new FixedTimeProvider(SAMPLE_TIME));

        var list = reader.ListMetrics();

        list.ObservedAtUtc.Should().Be(SAMPLE_TIME);
        list.Providers.Select(static provider => provider.ProviderId)
            .Should().Equal("alpha", "gamma");
        reader.GetMetrics("ALPHA").LookupState.Should().Be("available");
        reader.GetMetrics("ZETA").LookupState.Should().Be("disabled");
        reader.GetMetrics("pending").LookupState.Should().Be("pending");
        reader.GetMetrics("MOCK").LookupState.Should().Be("synthetic");
        reader.GetMetrics("missing").LookupState.Should().Be("unknown");
        reader.GetMetrics("missing").Provider.Should().BeNull();
    }

    /// <summary>Verifies an unsuccessful first reading has no invented success time.</summary>
    [Fact]
    public async Task FirstFailedReading_HasNoLastSuccessfulTime()
    {

        using var store = new UsageStore();
        Register(store, CreateSnapshot("claude", ProviderStatus.Stale));
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store);

        var provider = reader.GetMetrics("claude").Provider!;

        provider.Status.Should().Be("stale");
        provider.LastSuccessfulAtUtc.Should().BeNull();
        provider.LimitWindows.Should().BeEmpty();
    }

    /// <summary>Verifies blank provider IDs are rejected before store lookup.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Lookup_BlankProviderIdIsInvalid(string providerId)
    {

        using var store = new UsageStore();
        var reader = new McpMetricsReader(store);

        var act = () => reader.GetMetrics(providerId);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>Verifies retained stale values, nullable totals, and zero refresh side effects.</summary>
    [Fact]
    public async Task Reads_KeepStaleProvenanceAndNeverFetchProvider()
    {

        using var store = new UsageStore();
        var first = CreateSnapshot("claude") with
        {
            LimitWindows = [new LimitWindow { Name = "session", RemainingUnits = 7, UsedFraction = 0.8 }]
        };
        var later = CreateSnapshot("claude", ProviderStatus.Stale) with
        {
            FetchedAtUtc = SAMPLE_TIME.AddMinutes(5),
            ErrorDescription = "private diagnostic"
        };
        var provider = Register(store, first, later);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store, new FixedTimeProvider(SAMPLE_TIME));

        reader.ListMetrics();
        reader.GetMetrics("claude").Provider!.Status.Should().Be("ok");
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var stale = reader.GetMetrics("claude").Provider!;

        AssertRetainedStaleReading(stale);
        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
        JsonSerializer.Serialize(stale).Should().NotContain("private diagnostic");
    }

    /// <summary>Verifies safe billing, block, and Cline fields without owner or source filters.</summary>
    [Fact]
    public async Task Projection_ExposesAllowlistedProviderDetailsOnly()
    {

        using var store = new UsageStore();
        Register(store, CreateCopilotSnapshot());
        Register(store, CreateClineSnapshot());
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store);

        var list = reader.ListMetrics();
        var serialized = JsonSerializer.Serialize(list);

        list.Providers.Select(static provider => provider.ProviderId)
            .Should().Equal("cline", "copilot");
        list.Providers[0].ClineLocal!.TotalTokens.Should().Be(36);
        list.Providers[0].ClineAccount!.BalanceCredits.Should().Be(12.5);
        list.Providers[1].CopilotBilling!.Usage!.Remaining.Should().Be(5);
        list.Providers[1].ActiveBlock!.RetryAfterSeconds.Should().Be(30);
        serialized.Should().NotContain("private diagnostic");
        serialized.Should().NotContain("private reason");
        serialized.Should().NotContain("private principal");
        serialized.Should().NotContain("private owner");
        serialized.Should().NotContain("private filter");
        serialized.Should().NotContain("filters");
    }

    /// <summary>Verifies concurrent store updates and reads produce complete provider objects.</summary>
    [Fact]
    public async Task ConcurrentReads_DuringRefreshStayConsistent()
    {

        using var store = new UsageStore();
        var provider = Register(
            store,
            CreateSnapshot("claude"),
            CreateSnapshot("claude") with { FetchedAtUtc = SAMPLE_TIME.AddMinutes(1) },
            CreateSnapshot("claude", ProviderStatus.NeedsAuth) with { FetchedAtUtc = SAMPLE_TIME.AddMinutes(2) }
        );
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var reader = new McpMetricsReader(store);
        using var stopReads = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = Task.Run(() => ReadUntilStopped(reader, started, stopReads.Token), TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        stopReads.Cancel();
        await reads;
        AssertCoherentTimestamp(reader.GetMetrics("claude").Provider!);
        await provider.Received(3).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    private static void AssertCoherentTimestamp(McpProviderMetrics provider)
    {

        var expected = provider.Status == "ok" ? provider.SnapshotFetchedAtUtc : (DateTimeOffset?)null;
        provider.LastSuccessfulAtUtc.Should().Be(expected);
    }

    private static void ReadUntilStopped(McpMetricsReader reader, TaskCompletionSource started, CancellationToken stop)
    {

        while (!stop.IsCancellationRequested)
        {

            var result = reader.GetMetrics("claude");
            started.TrySetResult();
            result.LookupState.Should().Be("available");
            result.Provider!.ProviderId.Should().Be("claude");
            AssertCoherentTimestamp(result.Provider);
        }
    }

    private static IUsageProvider Register(UsageStore store, params Snapshot[] snapshots)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns(snapshots[0].ProviderId);
        var callIndex = -1;
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(snapshots[Math.Min(Interlocked.Increment(ref callIndex), snapshots.Length - 1)]));
        store.RegisterProvider(provider);

        return provider;
    }

    private static Snapshot CreateSnapshot(string providerId, ProviderStatus status = ProviderStatus.Ok)
    {

        return new Snapshot
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = SAMPLE_TIME,
            LimitWindows = []
        };
    }

    private static Snapshot CreateCopilotSnapshot()
    {

        return CreateSnapshot("copilot") with
        {
            ActiveBlock = new UsageBlock { IsBlocked = true, Reason = "private reason", RetryAfterSeconds = 30 },
            CopilotBilling = new CopilotBillingStatus
            {
                State = CopilotBillingState.Available,
                Reason = CopilotBillingReason.None,
                AttemptedAtUtc = SAMPLE_TIME,
                Usage = CreateCopilotUsage()
            },
            ErrorDescription = "private diagnostic"
        };
    }

    private static CopilotCreditUsage CreateCopilotUsage()
    {

        return new CopilotCreditUsage
        {
            Context = new CopilotBillingContext
            {
                PrincipalId = "private principal",
                OwnerId = "private owner",
                Scope = CopilotBillingScope.Personal,
                Plan = CopilotPlanType.Pro
            },
            Period = new CopilotBillingPeriod { IsVerified = true },
            Coverage = new CopilotReportCoverage
            {
                IsComplete = true,
                Filters = new Dictionary<string, string> { ["account"] = "private filter" }
            },
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = SAMPLE_TIME,
            Remaining = 5
        };
    }

    private static void AssertRetainedStaleReading(McpProviderMetrics stale)
    {

        stale.Status.Should().Be("stale");
        stale.SnapshotFetchedAtUtc.Should().Be(SAMPLE_TIME);
        stale.LastSuccessfulAtUtc.Should().Be(SAMPLE_TIME);
        stale.LimitWindows[0].RemainingUnits.Should().Be(7);
        stale.LimitWindows[0].UsedFraction.Should().BeNull();
    }

    private static Snapshot CreateClineSnapshot()
    {

        return CreateSnapshot("cline") with
        {
            ClineAccount = new ClineAccountUsage { BalanceCredits = 12.5, PlanName = "Pass", HasPassSubscription = true },
            ClineLocal = new ClineLocalUsage
            {
                InputTokens = 10,
                OutputTokens = 20,
                CacheReadTokens = 5,
                CacheWriteTokens = 1,
                ModelCalls = 2,
                WindowStartUtc = SAMPLE_TIME.AddHours(-1),
                LastActivityUtc = SAMPLE_TIME
            }
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => now;
    }
}
