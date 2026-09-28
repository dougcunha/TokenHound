using Serilog;
using System;
using System.Windows;
using TokenHound.App.Interop;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Owns the single update window and its view model: activate-or-create, placement, and one-time disposal.
/// Callers must invoke it on the UI thread.
/// </summary>
internal sealed class UpdateDialog
{
    private UpdateWindow? _window;
    private UpdateViewModel? _viewModel;

    /// <summary>Gets a value indicating whether the update window is open.</summary>
    public bool IsOpen
        => _window is not null;

    /// <summary>Shows the window, or restores and activates the open instance.</summary>
    /// <param name="owner">The owner window, or null.</param>
    /// <param name="viewModelFactory">Factory for the owned view model, used only when the window is created.</param>
    /// <returns>The view model bound to the open window.</returns>
    public UpdateViewModel Show(Window? owner, Func<UpdateViewModel> viewModelFactory)
    {

        ArgumentNullException.ThrowIfNull(viewModelFactory);

        if (_window is not null && _viewModel is not null)
        {
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;

            _window.Activate();

            return _viewModel;
        }

        var viewModel = viewModelFactory();
        var window = CreateWindow(owner, viewModel);

        window.Show();
        window.Activate();
        Log.Information("Update window opened");

        return viewModel;
    }

    private UpdateWindow CreateWindow(Window? owner, UpdateViewModel viewModel)
    {

        var window = new UpdateWindow { DataContext = viewModel };

        if (owner is not null && owner.IsLoaded)
            window.Owner = owner;

        WindowPlacement.PositionInWorkArea(window, owner);

        window.Closed += OnWindowClosed;
        _window = window;
        _viewModel = viewModel;

        return window;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {

        if (sender is Window window)
        {
            window.Closed -= OnWindowClosed;
            window.DataContext = null;
        }

        _window = null;

        var viewModel = _viewModel;
        _viewModel = null;
        viewModel?.Dispose();
        Log.Information("Update window closed");
    }
}
