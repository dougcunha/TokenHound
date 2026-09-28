using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Clock whose timers record their due time and fire only when the test says so, so delays can be asserted exactly.
/// </summary>
internal sealed class DelayTimeProvider : TimeProvider
{
    private readonly List<RecordedTimer> _timers = [];
    private readonly Lock _sync = new();

    /// <summary>Gets or sets the current instant.</summary>
    public DateTimeOffset UtcNow { get; set; }

    /// <summary>Gets the due times of every timer created so far, in creation order.</summary>
    public IReadOnlyList<TimeSpan> DueTimes
    {
        get
        {

            lock (_sync)
                return [.. _timers.ConvertAll(static timer => timer.DueTime)];
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
        => UtcNow;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {

        var timer = new RecordedTimer(callback, state, dueTime);

        lock (_sync)
            _timers.Add(timer);

        return timer;
    }

    /// <summary>Waits until at least <paramref name="count"/> timers exist.</summary>
    /// <param name="count">The number of timers to wait for.</param>
    /// <returns>A task that completes when enough timers exist, or fails after a timeout.</returns>
    public async Task WaitForTimersAsync(int count)
    {

        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DueTimes.Count < count)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Expected {count} timers, found {DueTimes.Count}.");

            await Task.Delay(5);
        }
    }

    /// <summary>Fires the timer at the given zero-based position.</summary>
    /// <param name="index">The creation-order index of the timer.</param>
    public void Fire(int index)
    {

        RecordedTimer timer;

        lock (_sync)
            timer = _timers[index];

        timer.Fire();
    }

    private sealed class RecordedTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        private bool _disposed;

        public TimeSpan DueTime { get; } = dueTime;

        public void Fire()
        {

            if (!_disposed)
                callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
            => !_disposed;

        public void Dispose()
            => _disposed = true;

        public ValueTask DisposeAsync()
        {

            Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
