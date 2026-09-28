using AwesomeAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Tests.ViewModels;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies the persisted GitHub rate-limit gate of the update checker (TC-08) and its state store.
/// </summary>
public sealed class UpdateRequestGateTests : IDisposable
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly string _directoryPath;
    private readonly ManualTimeProvider _clock = new(NOW);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRequestGateTests"/> class.
    /// </summary>
    public UpdateRequestGateTests()
    {

        _directoryPath = Path.Combine(Path.GetTempPath(), $"update_gate_test_{Guid.NewGuid():N}");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a fresh gate without persisted state allows dispatch.
    /// </summary>
    [Fact]
    public void CanDispatch_WithoutState_ReturnsTrue()
    {

        using var gate = CreateGate();

        gate.CanDispatch.Should().BeTrue();
        gate.ActiveDeadlineUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a 429 with Retry-After blocks until the deadline, also for a gate rebuilt from the same file.
    /// </summary>
    [Fact]
    public async Task RecordRateLimitAsync_WithRetryAfter_BlocksUntilDeadlineAcrossRestarts()
    {

        using var gate = CreateGate();

        await gate.RecordRateLimitAsync(120, null, TestContext.Current.CancellationToken);

        gate.ActiveDeadlineUtc.Should().BeOnOrAfter(NOW.AddSeconds(120));
        gate.CanDispatch.Should().BeFalse();

        using var reloaded = CreateGate();

        reloaded.CanDispatch.Should().BeFalse();
        reloaded.ActiveDeadlineUtc.Should().Be(gate.ActiveDeadlineUtc);
        reloaded.ConsecutiveFailures.Should().Be(1);

        _clock.Advance(reloaded.ActiveDeadlineUtc!.Value - NOW);

        reloaded.CanDispatch.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <c>Retry-After: 0</c> never produces an immediate retry.
    /// </summary>
    [Fact]
    public async Task RecordRateLimitAsync_WithZeroRetryAfter_AppliesFloor()
    {

        using var gate = CreateGate();

        await gate.RecordRateLimitAsync(0, null, TestContext.Current.CancellationToken);

        gate.ActiveDeadlineUtc.Should().BeOnOrAfter(NOW + RateLimitPolicy.MINIMUM_RETRY_FLOOR);
        gate.CanDispatch.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a 403 with an exhausted quota waits at least until the reported reset.
    /// </summary>
    [Fact]
    public async Task RecordRateLimitAsync_WithReset_WaitsUntilReset()
    {

        using var gate = CreateGate();
        var reset = NOW.AddMinutes(30);

        await gate.RecordRateLimitAsync(null, reset, TestContext.Current.CancellationToken);

        gate.ActiveDeadlineUtc.Should().BeOnOrAfter(reset);
        _clock.Advance(TimeSpan.FromMinutes(29));
        gate.CanDispatch.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a success clears the deadline and failure streak and records the check time.
    /// </summary>
    [Fact]
    public async Task RecordSuccessAsync_ClearsDeadlineAndStoresLastCheck()
    {

        using var gate = CreateGate();
        await gate.RecordRateLimitAsync(60, null, TestContext.Current.CancellationToken);
        _clock.Advance(TimeSpan.FromHours(2));

        await gate.RecordSuccessAsync(TestContext.Current.CancellationToken);

        var state = new UpdateStateStore(_directoryPath).Load();
        state.DeadlineUtc.Should().BeNull();
        state.ConsecutiveFailures.Should().Be(0);
        state.LastCheckUtc.Should().Be(NOW.AddHours(2));
        gate.LastCheckUtc.Should().Be(NOW.AddHours(2));
    }

    /// <summary>
    /// Verifies that a deadline that cannot be persisted blocks the gate for the process and surfaces an exception.
    /// </summary>
    [Fact]
    public async Task RecordRateLimitAsync_WhenPersistenceFails_BlocksProcess()
    {

        Directory.CreateDirectory(Path.GetDirectoryName(_directoryPath)!);
        await File.WriteAllTextAsync(_directoryPath, "not a directory", TestContext.Current.CancellationToken);

        try
        {
            using var gate = CreateGate();

            var act = () => gate.RecordRateLimitAsync(30, null, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<UpdateStatePersistenceException>();
            gate.IsProcessBlocked.Should().BeTrue();
            _clock.Advance(TimeSpan.FromDays(1));
            gate.CanDispatch.Should().BeFalse();
        }
        finally
        {
            File.Delete(_directoryPath);
        }
    }

    /// <summary>
    /// Verifies that a parseable deadline survives corrupt sibling fields and that invalid JSON yields an empty state.
    /// </summary>
    [Fact]
    public void Load_KeepsParseableDeadline()
    {

        Directory.CreateDirectory(_directoryPath);
        var path = Path.Combine(_directoryPath, UpdateStateStore.FILE_NAME);
        File.WriteAllText(path, """{"lastCheckUtc":"garbage","deadlineUtc":"2026-09-28T13:00:00.0000000+00:00","consecutiveFailures":"x"}""");

        var state = new UpdateStateStore(_directoryPath).Load();

        state.DeadlineUtc.Should().Be(NOW.AddHours(1));
        state.LastCheckUtc.Should().BeNull();
        state.ConsecutiveFailures.Should().Be(0);

        File.WriteAllText(path, "{ not json");
        new UpdateStateStore(_directoryPath).Load().Should().Be(new UpdateState());
    }

    private UpdateRequestGate CreateGate()
        => new(new UpdateStateStore(_directoryPath), _clock);
}
