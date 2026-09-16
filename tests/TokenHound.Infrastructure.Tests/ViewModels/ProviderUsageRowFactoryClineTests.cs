using AwesomeAssertions;
using System;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the Cline presentation rows: credits, local tokens, and the free model limit state.
/// </summary>
public sealed class ProviderUsageRowFactoryClineTests
{
    private const string CLINE_PROVIDER_ID = "cline";

    /// <summary>Verifies that a Cline account and local usage render as dedicated rows.</summary>
    [Fact]
    public void CreateRows_WithClineAccountAndLocal_RendersDedicatedRows()
    {

        var snapshot = new Snapshot
        {
            ProviderId = CLINE_PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = Now(),
            LimitWindows = [],
            ClineAccount = new ClineAccountUsage
            {
                BalanceCredits = 12.5,
                PlanName = "Cline Pass (Monthly)",
                HasPassSubscription = true
            },
            ClineLocal = new ClineLocalUsage
            {
                InputTokens = 100,
                OutputTokens = 20,
                CacheReadTokens = 5,
                CacheWriteTokens = 0,
                ModelCalls = 2,
                WindowStartUtc = Now().AddHours(-24),
                LastActivityUtc = Now()
            }
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(Now()));

        var credits = rows.Should().ContainSingle(static row => row.Key == "cline:credits").Subject;
        credits.Label.Should().Be("Cline credits");
        credits.UsedFraction.Should().BeNull();
        credits.PrimaryQuantityText.Should().Be("12.5 remaining");
        credits.ScopeText.Should().Be("Cline Pass (Monthly)");

        var local = rows.Should().ContainSingle(static row => row.Key == "cline:local").Subject;
        local.Label.Should().Be("Local tokens (24h)");
        local.PrimaryQuantityText.Should().Be("125 tokens");
        local.SecondaryQuantityText.Should().Be("2 model calls");
    }

    /// <summary>Verifies that an active free model limit block renders its reset countdown.</summary>
    [Fact]
    public void CreateRows_WithFreeLimitBlock_RendersLimitRow()
    {

        var now = Now();
        var snapshot = new Snapshot
        {
            ProviderId = CLINE_PROVIDER_ID,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows = [],
            ActiveBlock = new UsageBlock
            {
                Reason = "FreeModelLimitReached",
                IsBlocked = true,
                ResetTimeUtc = now.AddMinutes(90)
            }
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(now));

        var limit = rows.Should().ContainSingle(static row => row.Key == "cline:freelimit").Subject;
        limit.Label.Should().Be("Free model limit");
        limit.PrimaryQuantityText.Should().Be("Limit reached");
        limit.ResetText.Should().Be("Resets in 1h 30m");
    }

    /// <summary>Verifies that rows from other providers are untouched by the Cline path.</summary>
    [Fact]
    public void CreateRows_WithOtherProvider_SkipsClineRows()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "opencode",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = Now(),
            LimitWindows = [],
            ClineAccount = new ClineAccountUsage { BalanceCredits = 1.0 }
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(Now()));

        rows.Should().BeEmpty();
    }

    private static DateTimeOffset Now()
        => new(2026, 9, 16, 20, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}