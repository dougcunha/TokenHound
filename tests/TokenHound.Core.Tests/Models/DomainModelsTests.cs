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
        deserialized.LimitWindows[0].RemainingValue.Should().BeNull();
        deserialized.LimitWindows[1].UsedFraction.Should().BeNull();
        deserialized.ActiveBlock.Should().NotBeNull();
        deserialized.ActiveBlock!.IsBlocked.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Unsupported is distinct and nullable model fields round-trip through JSON.
    /// </summary>
    [Fact]
    public void Serialization_RoundTrip_PreservesUnsupportedAndRemainingValue()
    {
        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Unsupported,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Monthly Premium Interactions",
                    RemainingUnits = 10,
                    RemainingValue = 10.5,
                    TotalUnits = 100,
                    UsedFraction = 0.895
                }
            ]
        };

        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<Snapshot>(json);

        deserialized!.Status.Should().Be(ProviderStatus.Unsupported);
        deserialized.LimitWindows[0].RemainingValue.Should().Be(10.5);
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
