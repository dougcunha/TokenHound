using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies initial delay, tick cadence, due-ness, live settings, availability events, and cancellation of
/// <see cref="UpdateCheckScheduler"/> (TC-17).
/// </summary>
public sealed class UpdateCheckSchedulerTests
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly DelayTimeProvider _clock = new() { UtcNow = NOW };
    private UpdateSettings _settings = new();
    private DateTimeOffset? _lastCheckUtc;
    private UpdateCheckStatus _status = UpdateCheckStatus.UpToDate;
    private int _checks;

    /// <summary>Verifies the 60 s initial delay, the 5 min tick, and that a check runs at the first evaluation when due.</summary>
    [Fact]
    public async Task RunAsync_WaitsInitialDelayThenTicks_AndChecksWhenDue()
    {

        using var cts = new CancellationTokenSource();
        var run = CreateSut().RunAsync(cts.Token);

        await _clock.WaitForTimersAsync(1);
        _clock.DueTimes[0].Should().Be(TimeSpan.FromSeconds(60));
        _checks.Should().Be(0);

        _clock.Fire(0);
        await _clock.WaitForTimersAsync(2);

        _clock.DueTimes[1].Should().Be(TimeSpan.FromMinutes(5));
        _checks.Should().Be(1);

        await cts.CancelAsync();
        await run;
    }

    /// <summary>Verifies that cancelling the token ends the schedule quietly while it waits.</summary>
    [Fact]
    public async Task RunAsync_WhenCancelled_CompletesWithoutException()
    {

        using var cts = new CancellationTokenSource();
        var run = CreateSut().RunAsync(cts.Token);
        await _clock.WaitForTimersAsync(1);

        await cts.CancelAsync();

        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        run.IsCompletedSuccessfully.Should().BeTrue();
        _checks.Should().Be(0);
    }

    /// <summary>Verifies that no check runs until the interval since the last check has elapsed.</summary>
    [Fact]
    public async Task TickAsync_RunsOnlyOnceIntervalElapsed()
    {

        var sut = CreateSut();
        _lastCheckUtc = NOW.AddHours(-23);

        (await sut.TickAsync(CancellationToken.None)).Should().BeFalse();

        _clock.UtcNow = NOW.AddHours(1);

        (await sut.TickAsync(CancellationToken.None)).Should().BeTrue();
        _checks.Should().Be(1);
    }

    /// <summary>Verifies that a changed interval applies on the next tick without recreating the scheduler.</summary>
    [Fact]
    public async Task TickAsync_ReadsSettingsEveryTick()
    {

        var sut = CreateSut();
        _lastCheckUtc = NOW.AddHours(-2);

        (await sut.TickAsync(CancellationToken.None)).Should().BeFalse();

        _settings = new UpdateSettings { CheckIntervalHours = 1 };

        (await sut.TickAsync(CancellationToken.None)).Should().BeTrue();
    }

    /// <summary>Verifies that disabled updates and a zero interval never check.</summary>
    [Fact]
    public async Task TickAsync_WhenDisabledOrIntervalZero_NeverChecks()
    {

        var sut = CreateSut();

        _settings = new UpdateSettings { Enabled = false };
        (await sut.TickAsync(CancellationToken.None)).Should().BeFalse();

        _settings = new UpdateSettings { CheckIntervalHours = 0 };
        (await sut.TickAsync(CancellationToken.None)).Should().BeFalse();

        _checks.Should().Be(0);
    }

    /// <summary>Verifies that an available result raises the event once and other results raise none.</summary>
    [Fact]
    public async Task TickAsync_RaisesUpdateAvailableOnlyForAvailable()
    {

        var sut = CreateSut();
        var raised = new List<UpdateCheckOutcome>();
        sut.UpdateAvailable += (_, outcome) => raised.Add(outcome);

        foreach (var status in new[] { UpdateCheckStatus.UpToDate, UpdateCheckStatus.Skipped, UpdateCheckStatus.RateLimited, UpdateCheckStatus.Failed })
        {
            _status = status;
            await sut.TickAsync(CancellationToken.None);
        }

        raised.Should().BeEmpty();

        _status = UpdateCheckStatus.Available;
        await sut.TickAsync(CancellationToken.None);

        raised.Should().ContainSingle().Which.Status.Should().Be(UpdateCheckStatus.Available);
    }

    /// <summary>Verifies that a throwing check or settings read is contained and the tick reports no check.</summary>
    [Fact]
    public async Task TickAsync_WhenCheckOrSettingsThrow_ReturnsFalseWithoutThrowing()
    {

        var throwingCheck = new UpdateCheckScheduler(
            () => _settings,
            () => _lastCheckUtc,
            static _ => throw new InvalidOperationException("boom"),
            _clock
        );
        var throwingSettings = new UpdateCheckScheduler(
            static () => throw new InvalidOperationException("boom"),
            () => _lastCheckUtc,
            static _ => Task.FromResult(new UpdateCheckOutcome { Status = UpdateCheckStatus.UpToDate }),
            _clock
        );

        (await throwingCheck.TickAsync(CancellationToken.None)).Should().BeFalse();
        (await throwingSettings.TickAsync(CancellationToken.None)).Should().BeFalse();
    }

    private UpdateCheckScheduler CreateSut()
        => new(
            () => _settings,
            () => _lastCheckUtc,
            _ =>
            {
                _checks++;

                return Task.FromResult(new UpdateCheckOutcome { Status = _status });
            },
            _clock
        );
}
