using AwesomeAssertions;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Core.Tests.Models;

/// <summary>
/// Verifies behavior, immutability, Zero Fake Data enforcement, and serialization for core domain models and enums.
/// </summary>
public sealed class DomainModelsTests
{
    /// <summary>
    /// Verifies that LimitWindow keeps UsedFraction null when TotalUnits is null.
    /// </summary>
    [Fact]
    public void LimitWindow_WhenTotalUnitsIsNull_UsedFractionIsNull()
    {
        var windowWithoutFraction = new LimitWindow
        {
            Name = "Hourly",
            RemainingUnits = 500,
            TotalUnits = null
        };

        var windowWithIgnoredFraction = new LimitWindow
        {
            Name = "Daily",
            RemainingUnits = 500,
            TotalUnits = null,
            UsedFraction = 0.75
        };

        windowWithoutFraction.UsedFraction.Should().BeNull();
        windowWithIgnoredFraction.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that LimitWindow preserves explicit UsedFraction when TotalUnits is populated and does not invent fake fractions.
    /// </summary>
    [Fact]
    public void LimitWindow_WhenTotalUnitsIsProvided_PreservesUsedFraction()
    {
        var windowWithFraction = new LimitWindow
        {
            Name = "Daily",
            RemainingUnits = 20,
            TotalUnits = 100,
            UsedFraction = 0.8
        };

        var windowWithNullFraction = new LimitWindow
        {
            Name = "Monthly",
            RemainingUnits = 20,
            TotalUnits = 100,
            UsedFraction = null
        };

        windowWithFraction.UsedFraction.Should().Be(0.8);
        windowWithNullFraction.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies non-destructive mutation on LimitWindow using with expressions.
    /// </summary>
    [Fact]
    public void LimitWindow_SupportsWithExpressionAndImmutability()
    {
        var reset = DateTimeOffset.UtcNow.AddHours(1);
        var period = TimeSpan.FromHours(5);

        var original = new LimitWindow
        {
            Name = "Tokens",
            RemainingUnits = 50,
            TotalUnits = 100,
            UsedFraction = 0.5,
            ResetTimeUtc = reset,
            Period = period
        };

        var modified = original with { RemainingUnits = 40, UsedFraction = 0.6 };

        original.RemainingUnits.Should().Be(50);
        original.UsedFraction.Should().Be(0.5);
        modified.RemainingUnits.Should().Be(40);
        modified.UsedFraction.Should().Be(0.6);
        modified.Name.Should().Be("Tokens");
        modified.Period.Should().Be(period);
    }

    /// <summary>
    /// Verifies UsageBlock properties and immutability.
    /// </summary>
    [Fact]
    public void UsageBlock_PropertiesAndImmutability()
    {
        var reset = DateTimeOffset.UtcNow.AddMinutes(15);

        var block = new UsageBlock
        {
            Reason = "Rate limit exceeded",
            IsBlocked = true,
            ResetTimeUtc = reset,
            RetryAfterSeconds = 900
        };

        block.Reason.Should().Be("Rate limit exceeded");
        block.IsBlocked.Should().BeTrue();
        block.ResetTimeUtc.Should().Be(reset);
        block.RetryAfterSeconds.Should().Be(900);

        var unblocked = block with { IsBlocked = false, RetryAfterSeconds = 0 };

        block.IsBlocked.Should().BeTrue();
        unblocked.IsBlocked.Should().BeFalse();
    }

    /// <summary>
    /// Verifies AgentSession properties and state representation.
    /// </summary>
    [Fact]
    public void AgentSession_PropertiesAndState()
    {
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var activity = DateTimeOffset.UtcNow.AddMinutes(-1);

        var session = new AgentSession
        {
            Pid = 12345,
            StartTimeUtc = start,
            State = AgentSessionState.Busy,
            LastActivityUtc = activity
        };

        session.Pid.Should().Be(12345);
        session.StartTimeUtc.Should().Be(start);
        session.State.Should().Be(AgentSessionState.Busy);
        session.LastActivityUtc.Should().Be(activity);

        var idleSession = session with { State = AgentSessionState.Idle };

        session.State.Should().Be(AgentSessionState.Busy);
        idleSession.State.Should().Be(AgentSessionState.Idle);
    }

    /// <summary>
    /// Verifies Snapshot properties and collection handling.
    /// </summary>
    [Fact]
    public void Snapshot_PropertiesAndLimitWindowsCollection()
    {
        var fetchedAt = DateTimeOffset.UtcNow;
        var windows = new LimitWindow[]
        {
            new() { Name = "Requests", RemainingUnits = 10, TotalUnits = null }
        };

        var snapshot = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = fetchedAt,
            LimitWindows = windows,
            ActiveBlock = null,
            ErrorDescription = null
        };

        snapshot.ProviderId.Should().Be("claude");
        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.FetchedAtUtc.Should().Be(fetchedAt);
        snapshot.LimitWindows.Should().HaveCount(1);
        snapshot.ActiveBlock.Should().BeNull();
        snapshot.ErrorDescription.Should().BeNull();
    }

    /// <summary>
    /// Verifies JSON roundtrip serialization preserving types and the Zero Fake Data invariant.
    /// </summary>
    [Fact]
    public void Serialization_RoundTrip_PreservesAllModels()
    {
        var reset = DateTimeOffset.UtcNow.AddHours(2);
        var snapshot = CreateSampleSnapshot(reset);

        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<Snapshot>(json);

        deserialized.Should().NotBeNull();
        deserialized!.ProviderId.Should().Be("cursor");
        deserialized.Status.Should().Be(ProviderStatus.RateLimited);
        deserialized.LimitWindows.Should().HaveCount(2);
        deserialized.LimitWindows[0].UsedFraction.Should().Be(1.0);
        deserialized.LimitWindows[1].UsedFraction.Should().BeNull();
        deserialized.ActiveBlock.Should().NotBeNull();
        deserialized.ActiveBlock!.IsBlocked.Should().BeTrue();
    }

    /// <summary>
    /// Verifies all domain model types are sealed records.
    /// </summary>
    [Fact]
    public void DomainModels_AreSealedTypes()
    {
        typeof(LimitWindow).IsSealed.Should().BeTrue();
        typeof(UsageBlock).IsSealed.Should().BeTrue();
        typeof(AgentSession).IsSealed.Should().BeTrue();
        typeof(Snapshot).IsSealed.Should().BeTrue();
    }

    private static Snapshot CreateSampleSnapshot(DateTimeOffset reset)
    {
        var windows = new LimitWindow[]
        {
            new()
            {
                Name = "Day",
                RemainingUnits = 0,
                TotalUnits = 500,
                UsedFraction = 1.0,
                ResetTimeUtc = reset
            },
            new()
            {
                Name = "Fast",
                RemainingUnits = 15,
                TotalUnits = null,
                UsedFraction = null
            }
        };

        return new Snapshot
        {
            ProviderId = "cursor",
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows,
            ActiveBlock = new UsageBlock
            {
                Reason = "429",
                IsBlocked = true,
                ResetTimeUtc = reset,
                RetryAfterSeconds = 7200
            },
            ErrorDescription = "Rate limit hit"
        };
    }
}
