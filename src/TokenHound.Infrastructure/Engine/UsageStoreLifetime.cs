using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Coordinates admission, cancellation, background timer, draining, and resource disposal for <see cref="UsageStore"/>.
/// </summary>
internal sealed partial class UsageStoreLifetime : IDisposable
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly TaskCompletionSource _drainTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CancellationTokenSource? _timerCts;
    private Task? _timerTask;
    private CancellationTokenSource? _activityTimerCts;
    private Task? _activityTimerTask;
    private int _activeOperations;
    private volatile bool _isStopping;
    private volatile bool _isDisposed;
    private Task? _stopTask;

    /// <summary>Gets a value indicating whether the periodic timer is running.</summary>
    public bool IsTimerRunning
        => _timerCts is { IsCancellationRequested: false };

    /// <summary>Gets a value indicating whether the store has initiated terminal stopping or has been disposed.</summary>
    public bool IsStoppingOrDisposed
        => _isStopping || _isDisposed;

    /// <summary>Starts the background periodic polling timer.</summary>
    /// <param name="interval">The tick interval.</param>
    /// <param name="onTick">The action to invoke on each tick.</param>
    public void StartTimer(TimeSpan interval, Func<CancellationToken, Task> onTick)
    {

        CancellationTokenSource? oldCts;
        Task? oldTask;

        lock (_lock)
        {

            if (_isStopping || _isDisposed)
                throw new ObjectDisposedException(nameof(UsageStore), "The usage store is stopping or has been disposed.");

            oldCts = _timerCts;
            oldTask = _timerTask;

            var newCts = new CancellationTokenSource();
            _timerCts = newCts;
            var newTask = RunTimerLoopAsync(
                interval,
                onTick,
                newCts,
                _stoppingCts.Token
            );
            _timerTask = oldTask is null || oldTask.IsCompleted
                ? newTask
                : Task.WhenAll(oldTask, newTask);
        }

        CleanCts(oldCts, true);
    }

    /// <summary>Stops the background periodic polling timer.</summary>
    public void StopTimer()
    {

        CancellationTokenSource? cts;

        lock (_lock)
        {

            cts = _timerCts;
            _timerCts = null;
        }

        CleanCts(cts, true);
    }

    /// <summary>
    /// Enters an admitted operation scope under the lifecycle lock.
    /// </summary>
    /// <param name="cancellationToken">The caller's cancellation token to link.</param>
    /// <returns>A disposable lease that releases the admission count and linked token source.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the store is stopping or disposed.</exception>
    public OperationLease Enter(CancellationToken cancellationToken = default)
    {

        lock (_lock)
        {

            if (_isStopping || _isDisposed)
                throw new ObjectDisposedException(nameof(UsageStore), "The usage store is stopping or has been disposed.");

            _activeOperations++;
        }

        try
        {

            var linkedCts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token)
                : null;

            return new OperationLease(this, linkedCts, linkedCts?.Token ?? _stoppingCts.Token);
        }
        catch
        {

            Exit();
            throw;
        }
    }

    /// <summary>
    /// Requests terminal stopping, cancels admitted work, drains background work, and disposes resources.
    /// </summary>
    /// <param name="disposeResources">An optional delegate to release synchronization resources after draining.</param>
    /// <param name="cancellationToken">A token to observe for canceling the wait.</param>
    /// <returns>A task representing the terminal stop operation.</returns>
    public Task StopAsync(
        Action? disposeResources = null,
        CancellationToken cancellationToken = default)
    {

        Task stopTask;

        lock (_lock)
        {

            if (_stopTask is not null)
                stopTask = _stopTask;
            else
            {

                _isStopping = true;
                _stoppingCts.Cancel();

                if (_activeOperations == 0)
                    _drainTcs.TrySetResult();

                stopTask = _stopTask = RunStopAsync(disposeResources);
            }
        }

        return cancellationToken.CanBeCanceled
            ? stopTask.WaitAsync(cancellationToken)
            : stopTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        lock (_lock)
        {

            if (_isDisposed)
                return;

            _isDisposed = true;
        }

        StopTimer();
        StopActivityTimer();
        CleanCts(_stoppingCts, false);
    }

    internal void Exit()
    {

        lock (_lock)
        {

            _activeOperations--;

            if (_isStopping && _activeOperations == 0)
                _drainTcs.TrySetResult();
        }
    }

    private static Task RunTimerLoopAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> onTick,
        CancellationToken cancellationToken)
        => RunTimerLoopAsync(interval, onTick, null, cancellationToken);

    private static async Task RunTimerLoopAsync(
        TimeSpan interval,
        Func<CancellationToken, Task> onTick,
        CancellationTokenSource? timerCts,
        CancellationToken stoppingToken)
    {

        using var timer = new PeriodicTimer(interval);
        var timerToken = timerCts?.Token ?? stoppingToken;

        try
        {

            while (await timer.WaitForNextTickAsync(timerToken).ConfigureAwait(false))
            {

                stoppingToken.ThrowIfCancellationRequested();
                await onTick(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
        }
        finally
        {

            CleanCts(timerCts, false);
        }
    }

    private async Task RunStopAsync(Action? disposeResources)
    {

        try
        {

            await Task.WhenAll(_drainTcs.Task, DrainTimerAsync(false), DrainTimerAsync(true)).ConfigureAwait(false);
        }
        catch
        {
        }

        lock (_lock)
            _isDisposed = true;

        CleanCts(_stoppingCts, false);
        disposeResources?.Invoke();
    }

    private async Task DrainTimerAsync(bool activity)
    {

        var timer = TakeTimer(activity);

        CleanCts(timer.CancellationSource, true);

        if (timer.Task is not null)
        {

            try
            {

                await timer.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        CleanCts(timer.CancellationSource, false);
    }

    private static void CleanCts(CancellationTokenSource? cts, bool cancel)
    {

        try
        {

            if (cancel)
                cts?.Cancel();
            else
                cts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>Represents an active admitted operation lease.</summary>
    public readonly struct OperationLease(
        UsageStoreLifetime lifetime,
        CancellationTokenSource? linkedCts,
        CancellationToken token) : IDisposable
    {
        /// <summary>Gets the effective cancellation token for the operation.</summary>
        public CancellationToken Token { get; } = token;

        /// <inheritdoc />
        public void Dispose()
        {

            linkedCts?.Dispose();
            lifetime.Exit();
        }
    }
}
