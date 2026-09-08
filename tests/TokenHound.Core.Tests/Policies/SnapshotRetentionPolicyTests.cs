using AwesomeAssertions;
using System;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies pure last-good snapshot retention and history-clearing decisions.
/// </summary>
public sealed class SnapshotRetentionPolicyTests
{
    private static readonly DateTimeOffset ORIGINAL_FETCH = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a successful snapshot replaces the current and archived readings.
    /// </summary>
    [Fact]
    public void Apply_WhenSnapshotIsSuccessful_ReplacesCurrentAndArchive()
    {
        var incoming = CreateSnapshot(ProviderStatus.Ok, DateTimeOffset.UtcNow);

        var decision = SnapshotRetentionPolicy.Apply(incoming, null);

        decision.CurrentSnapshot.Should().Be(incoming);
        decision.ArchivedSnapshot.Should().Be(incoming);
        decision.ClearsHistory.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that transient failure retains the original successful data and fetch time.
    /// </summary>
    [Fact]
    public void Apply_WhenSnapshotIsStale_RetainsLastGoodDataAndUsesNewBlock()
    {
        var lastGood = CreateSnapshot(ProviderStatus.Ok, ORIGINAL_FETCH);
        var block = new UsageBlock
        {
            Reason = "429",
            IsBlocked = true,
            ResetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(1),
            RetryAfterSeconds = 60
        };
        var incoming = CreateSnapshot(ProviderStatus.RateLimited, DateTimeOffset.UtcNow) with
        {
            ActiveBlock = block,
            ErrorDescription = "Rate limited"
        };

        var decision = SnapshotRetentionPolicy.Apply(incoming, lastGood);

        decision.CurrentSnapshot.Status.Should().Be(ProviderStatus.Stale);
        decision.CurrentSnapshot.FetchedAtUtc.Should().Be(ORIGINAL_FETCH);
        decision.CurrentSnapshot.LimitWindows.Should().BeSameAs(lastGood.LimitWindows);
        decision.CurrentSnapshot.ActiveBlock.Should().Be(block);
        decision.ArchivedSnapshot.Should().Be(lastGood);
        decision.ClearsHistory.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a transient failure without history remains stale without fabricated quota data.
    /// </summary>
    [Fact]
    public void Apply_WhenStaleHasNoHistory_ReturnsEmptyStaleSnapshot()
    {
        var incoming = CreateSnapshot(ProviderStatus.Stale, DateTimeOffset.UtcNow);

        var decision = SnapshotRetentionPolicy.Apply(incoming, null);

        decision.CurrentSnapshot.Status.Should().Be(ProviderStatus.Stale);
        decision.CurrentSnapshot.LimitWindows.Should().BeEmpty();
        decision.ArchivedSnapshot.Should().BeNull();
    }

    /// <summary>
    /// Verifies that authentication failure remains visible while clearing history.
    /// </summary>
    [Fact]
    public void Apply_WhenAuthenticationIsRequired_ClearsHistoryButPreservesStatus()
    {
        var incoming = CreateSnapshot(ProviderStatus.NeedsAuth, DateTimeOffset.UtcNow);

        var decision = SnapshotRetentionPolicy.Apply(incoming, CreateSnapshot(ProviderStatus.Ok, ORIGINAL_FETCH));

        decision.CurrentSnapshot.Should().Be(incoming);
        decision.ArchivedSnapshot.Should().BeNull();
        decision.ClearsHistory.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an unsupported provider state has the same explicit history-clearing behavior.
    /// </summary>
    [Fact]
    public void Apply_WhenUnsupported_ClearsHistoryButPreservesStatus()
    {
        var incoming = CreateSnapshot(ProviderStatus.Unsupported, DateTimeOffset.UtcNow);

        var decision = SnapshotRetentionPolicy.Apply(incoming, CreateSnapshot(ProviderStatus.Ok, ORIGINAL_FETCH));

        decision.CurrentSnapshot.Status.Should().Be(ProviderStatus.Unsupported);
        decision.ArchivedSnapshot.Should().BeNull();
        decision.ClearsHistory.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that incoming Copilot billing state is preserved across all status branches,
    /// and that archived snapshots have billing stripped.
    /// </summary>
    [Theory]
    [InlineData(ProviderStatus.Ok)]
    [InlineData(ProviderStatus.Stale)]
    [InlineData(ProviderStatus.NeedsAuth)]
    [InlineData(ProviderStatus.Unsupported)]
    public void Apply_PreservesIncomingCopilotBillingAcrossRetentionBranches(ProviderStatus incomingStatus)
    {
        var billing = new CopilotBillingStatus
        {
            State = CopilotBillingState.Available,
            Reason = CopilotBillingReason.None,
            AttemptedAtUtc = DateTimeOffset.UtcNow
        };

        var lastGood = CreateSnapshot(ProviderStatus.Ok, ORIGINAL_FETCH);
        var incoming = CreateSnapshot(incomingStatus, DateTimeOffset.UtcNow) with
        {
            CopilotBilling = billing
        };

        var decision = SnapshotRetentionPolicy.Apply(incoming, lastGood);

        decision.CurrentSnapshot.CopilotBilling.Should().Be(billing);

        if (decision.ArchivedSnapshot is not null)
            decision.ArchivedSnapshot.CopilotBilling.Should().BeNull();
    }

    /// <summary>
    /// Verifies that generic retention never resurrects an absent billing payload.
    /// </summary>
    [Fact]
    public void Apply_WhenIncomingBillingIsNull_NeverResurrectsPriorBilling()
    {
        var priorBilling = new CopilotBillingStatus
        {
            State = CopilotBillingState.Available,
            Reason = CopilotBillingReason.None,
            AttemptedAtUtc = ORIGINAL_FETCH
        };

        var lastGood = CreateSnapshot(ProviderStatus.Ok, ORIGINAL_FETCH) with
        {
            CopilotBilling = priorBilling
        };

        var incoming = CreateSnapshot(ProviderStatus.Stale, DateTimeOffset.UtcNow) with
        {
            CopilotBilling = null
        };

        var decision = SnapshotRetentionPolicy.Apply(incoming, lastGood);

        decision.CurrentSnapshot.CopilotBilling.Should().BeNull();
    }

    private static Snapshot CreateSnapshot(ProviderStatus status, DateTimeOffset fetchedAtUtc)
        => new()
        {
            ProviderId = "copilot",
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = fetchedAtUtc,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Monthly Premium Interactions",
                    RemainingUnits = 25,
                    RemainingValue = 25.5,
                    TotalUnits = 100,
                    UsedFraction = 0.745
                }
            ]
        };
}
