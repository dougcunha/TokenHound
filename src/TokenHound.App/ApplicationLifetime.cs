using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.App;

/// <summary>
/// Coordinates application shutdown, lifetime cancellation, engine draining, and resource disposal.
/// </summary>
public sealed class ApplicationLifetime : IDisposable
{
    private readonly UsageStore _usageStore;
    private readonly DialogService _dialogService;
    private readonly NotchViewModel _notchViewModel;
    private readonly IReadOnlyList<IDisposable> _disposableResources;
    private readonly Action _shutdownAction;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _syncLock = new();

    private Task? _startupTask;
    private Task? _shutdownTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationLifetime"/> class.
    /// </summary>
    /// <param name="usageStore">The central usage store coordinator.</param>
    /// <param name="dialogService">The dialog service managing active dialog windows.</param>
    /// <param name="notchViewModel">The notch HUD view model.</param>
    /// <param name="disposableResources">Application-owned disposable resources to clean up on shutdown.</param>
    /// <param name="shutdownAction">Action invoked on the dispatcher to terminate the WPF application.</param>
    public ApplicationLifetime(
        UsageStore usageStore,
        DialogService dialogService,
        NotchViewModel notchViewModel,
        IReadOnlyList<IDisposable> disposableResources,
        Action? shutdownAction = null)
    {

        ArgumentNullException.ThrowIfNull(usageStore);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(notchViewModel);
        ArgumentNullException.ThrowIfNull(disposableResources);

        _usageStore = usageStore;
        _dialogService = dialogService;
        _notchViewModel = notchViewModel;
        _disposableResources = disposableResources;
        _shutdownAction = shutdownAction ?? DefaultShutdown;
    }

    /// <summary>
    /// Gets a cancellation token tied to the application lifetime.
    /// </summary>
    public CancellationToken LifetimeToken
        => _cts.Token;

    /// <summary>
    /// Tracks the asynchronous startup refresh task for graceful drainage on shutdown.
    /// </summary>
    /// <param name="task">The startup task to track.</param>
    public void TrackStartupTask(Task task)
    {

        ArgumentNullException.ThrowIfNull(task);
        _startupTask = task;
    }

    /// <summary>
    /// Initiates coordinated application shutdown and draining of active operations.
    /// </summary>
    /// <returns>A task representing the shutdown operation.</returns>
    public Task ShutdownAsync()
    {

        lock (_syncLock)
        {

            if (_shutdownTask is not null)
                return _shutdownTask;

            _shutdownTask = ShutdownCoreAsync();
        }

        return _shutdownTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;

        if (_shutdownTask is null)
            _ = ShutdownAsync();
    }

    private async Task ShutdownCoreAsync()
    {

        Log.Information("Initiating application shutdown and resource cleanup...");

        _cts.Cancel();
        _dialogService.CloseAll();
        _notchViewModel.Dispose();

        await DrainStartupTaskAsync().ConfigureAwait(false);
        await DrainUsageStoreAsync().ConfigureAwait(false);
        DisposeResources();

        _cts.Dispose();

        Log.Information("Application shutdown completed successfully.");
        _shutdownAction();
    }

    private async Task DrainStartupTaskAsync()
    {

        if (_startupTask is null)
            return;

        try
        {

            await _startupTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {

            Log.Warning(ex, "Exception caught while draining startup refresh task");
        }
    }

    private async Task DrainUsageStoreAsync()
    {

        try
        {

            await _usageStore.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {

            Log.Warning(ex, "Exception caught while stopping UsageStore");
        }

        _usageStore.Dispose();
    }

    private void DisposeResources()
    {

        foreach (var resource in _disposableResources)
        {

            try
            {

                resource.Dispose();
            }
            catch (Exception ex)
            {

                Log.Warning(ex, "Exception caught while disposing application resource");
            }
        }
    }

    private static void DefaultShutdown()
    {

        var app = Application.Current;

        if (app is null)
            return;

        if (app.Dispatcher.CheckAccess())
            app.Shutdown();
        else
            app.Dispatcher.Invoke(app.Shutdown);
    }
}
