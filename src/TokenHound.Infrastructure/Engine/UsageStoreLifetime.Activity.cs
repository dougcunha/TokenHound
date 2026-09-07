using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

internal sealed partial class UsageStoreLifetime
{
    /// <summary>
    /// Gets a value indicating whether the separate activity timer is running.
    /// </summary>
    public bool IsActivityTimerRunning
        => _activityTimerTask is not null && !(_activityTimerCts?.IsCancellationRequested ?? true);

    /// <summary>
    /// Starts the separate activity polling timer.
    /// </summary>
    /// <param name="interval">The activity polling interval.</param>
    /// <param name="onTick">The action to invoke on each activity tick.</param>
    public void StartActivityTimer(TimeSpan interval, Func<CancellationToken, Task> onTick)
    {

        StopActivityTimer();

        lock (_lock)
        {

            if (_isStopping || _isDisposed)
                throw new ObjectDisposedException(nameof(UsageStore), "The usage store is stopping or has been disposed.");

            var cts = new CancellationTokenSource();
            _activityTimerCts = cts;
            _activityTimerTask = RunTimerLoopAsync(interval, onTick, cts.Token);
        }
    }

    /// <summary>
    /// Stops the separate activity polling timer.
    /// </summary>
    public void StopActivityTimer()
    {

        CancellationTokenSource? cts;

        lock (_lock)
        {

            cts = _activityTimerCts;
            _activityTimerCts = null;
            _activityTimerTask = null;
        }

        cts?.Cancel();
        cts?.Dispose();
    }

    private (CancellationTokenSource? CancellationSource, Task? Task) TakeTimer(bool activity)
    {

        lock (_lock)
        {

            if (activity)
            {
                var timer = (_activityTimerCts, _activityTimerTask);
                _activityTimerCts = null;
                _activityTimerTask = null;

                return timer;
            }

            var quotaTimer = (_timerCts, _timerTask);
            _timerCts = null;
            _timerTask = null;

            return quotaTimer;
        }
    }
}
