using Serilog;
using System;
using System.Windows;
using TokenHound.App.Interop;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Owns the single provider status window and its view model: activate-or-create, placement, and one-time disposal.
/// Callers must invoke it on the UI thread.
/// </summary>
internal sealed class ProviderStatusDialog
{
    private ProviderStatusWindow? _window;
    private ProviderStatusViewModel? _viewModel;

    /// <summary>Gets a value indicating whether the provider status window is open.</summary>
    public bool IsOpen
        => _window is not null;

    /// <summary>Shows the window, or restores and activates the open instance.</summary>
    /// <param name="owner">The owner window, or null.</param>
    /// <param name="viewModelFactory">Factory for the owned view model, or null to open without data context.</param>
    public void Show(Window? owner, Func<ProviderStatusViewModel>? viewModelFactory)
    {

        if (_window is not null)
        {
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;

            _window.Activate();

            return;
        }

        var window = CreateWindow(owner, viewModelFactory);

        window.Show();
        window.Activate();
        Log.Information("Provider status window opened");
    }

    /// <summary>Closes the window if open and disposes its view model once.</summary>
    public void Close()
    {

        var window = _window;
        _window = null;

        if (window is not null)
        {
            window.Closed -= OnWindowClosed;
            window.DataContext = null;
            window.Close();
            Log.Information("Provider status window closed");
        }

        DisposeViewModel();
    }

    private ProviderStatusWindow CreateWindow(Window? owner, Func<ProviderStatusViewModel>? viewModelFactory)
    {

        var window = new ProviderStatusWindow();
        var viewModel = viewModelFactory?.Invoke();

        if (viewModel is not null)
            window.DataContext = viewModel;

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

        _window = null;
        DisposeViewModel();
        Log.Information("Provider status window closed");
    }

    private void DisposeViewModel()
    {

        var viewModel = _viewModel;
        _viewModel = null;
        viewModel?.Dispose();
    }
}
