using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Serilog;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Tray;

/// <summary>
/// Presentation view model for the notification-area icon context menu, translating user menu actions
/// into visibility, refresh, dialog, and shutdown operations.
/// </summary>
public sealed class TrayIconViewModel : INotifyPropertyChanged
{
    private readonly HudActionsViewModel _hudActions;
    private readonly NotchVisibilityController _visibility;
    private readonly TrayMenuModel _menu;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconViewModel"/> class.
    /// </summary>
    /// <param name="hudActions">The HUD actions view model coordinating presentation actions.</param>
    /// <param name="visibility">The Notch visibility controller.</param>
    /// <param name="menu">The tray menu model defining menu structure and copy.</param>
    public TrayIconViewModel(
        HudActionsViewModel hudActions,
        NotchVisibilityController visibility,
        TrayMenuModel menu)
        : this(hudActions, visibility, menu, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconViewModel"/> class with an optional logger.
    /// </summary>
    /// <param name="hudActions">The HUD actions view model coordinating presentation actions.</param>
    /// <param name="visibility">The Notch visibility controller.</param>
    /// <param name="menu">The tray menu model defining menu structure and copy.</param>
    /// <param name="logger">Optional logger instance; defaults to the ambient <see cref="Log.Logger"/>.</param>
    public TrayIconViewModel(
        HudActionsViewModel hudActions,
        NotchVisibilityController visibility,
        TrayMenuModel menu,
        ILogger? logger)
    {

        ArgumentNullException.ThrowIfNull(hudActions);
        ArgumentNullException.ThrowIfNull(visibility);
        ArgumentNullException.ThrowIfNull(menu);

        _hudActions = hudActions;
        _visibility = visibility;
        _menu = menu;
        _logger = logger ?? Log.Logger;

        _visibility.VisibilityChanged += OnVisibilityChanged;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the current display header for the visibility-toggle menu item.
    /// </summary>
    public string ToggleHeader
        => TrayMenuModel.ResolveToggleHeader(_visibility.IsNotchVisible);

    /// <summary>
    /// Builds the complete tray context-menu descriptor for the current Notch visibility state.
    /// </summary>
    /// <returns>A new <see cref="TrayMenuDescriptor"/> reflecting current visibility.</returns>
    public TrayMenuDescriptor BuildMenu()
        => new() { Entries = _menu.BuildDescriptor(_visibility.IsNotchVisible) };

    /// <summary>
    /// Invokes the action associated with the specified menu item key.
    /// </summary>
    /// <param name="key">The key identifying which action to invoke.</param>
    public void Invoke(TrayMenuItemKey key)
    {

        switch (key)
        {
            case TrayMenuItemKey.ToggleNotch:
                ToggleNotch();
                break;

            case TrayMenuItemKey.RefreshNow:
                _ = RefreshNow();
                break;

            case TrayMenuItemKey.Settings:
                ShowSettings();
                break;

            case TrayMenuItemKey.About:
                ShowAbout();
                break;

            case TrayMenuItemKey.Exit:
                _ = Exit();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }

    /// <summary>
    /// Toggles the Notch visibility state between visible and hidden.
    /// </summary>
    public void ToggleNotch()
    {

        _logger.Information("Tray action {Action} invoked", nameof(ToggleNotch));
        _visibility.Toggle();
    }

    /// <summary>
    /// Initiates an immediate refresh of provider usage data.
    /// </summary>
    /// <returns>A task representing the refresh operation.</returns>
    public Task RefreshNow()
    {

        _logger.Information("Tray action {Action} invoked", nameof(RefreshNow));

        return _hudActions.RefreshAsync();
    }

    /// <summary>
    /// Displays the modeless Settings dialog.
    /// </summary>
    public void ShowSettings()
    {

        _logger.Information("Tray action {Action} invoked", nameof(ShowSettings));
        _hudActions.ShowSettings();
    }

    /// <summary>
    /// Displays the modeless About dialog.
    /// </summary>
    public void ShowAbout()
    {

        _logger.Information("Tray action {Action} invoked", nameof(ShowAbout));
        _hudActions.ShowAbout();
    }

    /// <summary>
    /// Initiates the coordinated application shutdown.
    /// </summary>
    /// <returns>A task representing the terminal shutdown operation.</returns>
    public Task Exit()
    {

        _logger.Information("Tray action {Action} invoked", nameof(Exit));

        return _hudActions.ShutdownAsync();
    }

    private void OnVisibilityChanged(object? sender, EventArgs e)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToggleHeader)));
}
