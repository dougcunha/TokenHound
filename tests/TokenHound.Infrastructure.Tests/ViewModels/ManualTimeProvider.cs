using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Deterministic clock in UTC whose timers fire only when the test advances time.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow;

    /// <summary>Initializes a new instance of the <see cref="ManualTimeProvider"/> class.</summary>
    /// <param name="utcNow">The initial current instant.</param>
    public ManualTimeProvider(DateTimeOffset utcNow)
    {

        _utcNow = utcNow;
    }

    /// <summary>Gets the number of timers created and not yet disposed.</summary>
    public int ActiveTimerCount
        => _timers.FindAll(static timer => !timer.IsDisposed).Count;

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone
        => TimeZoneInfo.Utc;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
        => _utcNow;

    /// <inheritdoc />
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {

        var timer = new ManualTimer(callback, state);
        _timers.Add(timer);

        return timer;
    }

    /// <summary>Moves the clock forward and fires every active timer once.</summary>
    /// <param name="delta">The time to advance.</param>
    public void Advance(TimeSpan delta)
    {

        _utcNow += delta;

        foreach (var timer in _timers.ToArray())
            timer.Fire();
    }

    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        public bool IsDisposed { get; private set; }

        public void Fire()
        {

            if (!IsDisposed)
                callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
            => !IsDisposed;

        public void Dispose()
            => IsDisposed = true;

        public ValueTask DisposeAsync()
        {

            Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
