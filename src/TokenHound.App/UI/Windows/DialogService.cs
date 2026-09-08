using System;
using System.Windows;
using TokenHound.App.Interop;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Manages modeless lifecycle, owner binding, placement, and activation for application dialog windows.
/// </summary>
public sealed class DialogService
{
    private readonly Func<Window?>? _ownerProvider;
    private SettingsWindow? _settingsWindow;
    private SettingsViewModel? _settingsViewModel;
    private AboutWindow? _aboutWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    /// <param name="ownerProvider">Optional delegate that provides the owner window reference.</param>
    public DialogService(Func<Window?>? ownerProvider = null)
    {

        _ownerProvider = ownerProvider;
    }

    /// <summary>
    /// Gets a value indicating whether the Settings dialog is currently open.
    /// </summary>
    public bool IsSettingsOpen
        => _settingsWindow is not null;

    /// <summary>
    /// Gets a value indicating whether the About dialog is currently open.
    /// </summary>
    public bool IsAboutOpen
        => _aboutWindow is not null;

    /// <summary>
    /// Shows the Settings dialog modelessly, or activates and restores the existing instance if already open.
    /// </summary>
    /// <param name="owner">Optional owner window overriding the default owner provider.</param>
    /// <param name="viewModelFactory">
    /// Optional factory producing the view model bound to the dialog. The service owns the produced instance
    /// and disposes it exactly once when the dialog closes. When omitted the dialog opens without a data context.
    /// </param>
    public void ShowSettings(Window? owner = null, Func<SettingsViewModel>? viewModelFactory = null)
    {

        var app = Application.Current;

        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => ShowSettings(owner, viewModelFactory));

            return;
        }

        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;

            _settingsWindow.Activate();

            return;
        }

        var effectiveOwner = owner ?? _ownerProvider?.Invoke() ?? Application.Current?.MainWindow;
        var window = CreateSettingsWindow(effectiveOwner, viewModelFactory);

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Closes the Settings dialog if currently open.
    /// </summary>
    public void CloseSettings()
    {

        var app = Application.Current;

        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(CloseSettings);

            return;
        }

        var window = _settingsWindow;
        _settingsWindow = null;

        if (window is not null)
        {
            window.Closed -= OnSettingsWindowClosed;
            window.DataContext = null;
            window.Close();
        }

        DisposeSettingsViewModel();
    }

    /// <summary>
    /// Shows the About dialog modelessly, or activates and restores the existing instance if already open.
    /// </summary>
    /// <param name="owner">Optional owner window overriding the default owner provider.</param>
    public void ShowAbout(Window? owner = null)
    {

        var app = Application.Current;

        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => ShowAbout(owner));

            return;
        }

        if (_aboutWindow is not null)
        {
            if (_aboutWindow.WindowState == WindowState.Minimized)
                _aboutWindow.WindowState = WindowState.Normal;

            _aboutWindow.Activate();

            return;
        }

        var effectiveOwner = owner ?? _ownerProvider?.Invoke() ?? Application.Current?.MainWindow;
        var window = new AboutWindow();

        if (effectiveOwner is not null && effectiveOwner.IsLoaded)
            window.Owner = effectiveOwner;

        WindowPlacement.PositionInWorkArea(window, effectiveOwner);

        window.Closed += OnAboutWindowClosed;
        _aboutWindow = window;

        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Closes the About dialog if currently open.
    /// </summary>
    public void CloseAbout()
    {

        var app = Application.Current;

        if (app is not null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(CloseAbout);

            return;
        }

        if (_aboutWindow is null)
            return;

        var window = _aboutWindow;
        _aboutWindow = null;
        window.Closed -= OnAboutWindowClosed;
        window.Close();
    }

    /// <summary>
    /// Closes all active dialog windows managed by this service.
    /// </summary>
    public void CloseAll()
    {

        CloseSettings();
        CloseAbout();
    }

    private SettingsWindow CreateSettingsWindow(Window? effectiveOwner, Func<SettingsViewModel>? viewModelFactory)
    {

        var window = new SettingsWindow();
        var viewModel = viewModelFactory?.Invoke();

        if (viewModel is not null)
            window.DataContext = viewModel;

        if (effectiveOwner is not null && effectiveOwner.IsLoaded)
            window.Owner = effectiveOwner;

        WindowPlacement.PositionInWorkArea(window, effectiveOwner);

        window.Closed += OnSettingsWindowClosed;
        _settingsWindow = window;
        _settingsViewModel = viewModel;

        return window;
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {

        _settingsWindow = null;
        DisposeSettingsViewModel();
    }

    /// <summary>
    /// Releases the Settings view model and clears the field before disposing, so the closed-event path and the
    /// explicit close path can never dispose the same instance twice nor leave one behind.
    /// </summary>
    private void DisposeSettingsViewModel()
    {

        var viewModel = _settingsViewModel;
        _settingsViewModel = null;
        viewModel?.Dispose();
    }

    private void OnAboutWindowClosed(object? sender, EventArgs e)
    {

        _aboutWindow = null;
    }
}
