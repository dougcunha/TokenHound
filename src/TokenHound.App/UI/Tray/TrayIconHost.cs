using System;
using Serilog;

namespace TokenHound.App.UI.Tray;

/// <summary>
/// Coordinates notification-area icon lifecycle, graceful-degradation fallback, event subscription,
/// and thread-safe UI marshalling.
/// </summary>
public sealed class TrayIconHost : IDisposable
{
    private readonly ITrayIcon _trayIcon;
    private readonly TrayIconViewModel _viewModel;
    private readonly Action<Action> _dispatch;
    private readonly Uri _iconSource;
    private readonly ILogger _logger;

    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconHost"/> class.
    /// </summary>
    /// <param name="trayIcon">The notification-area icon abstraction.</param>
    /// <param name="viewModel">The tray view model handling user actions.</param>
    /// <param name="dispatch">The dispatcher action used to marshal tray events to the UI thread.</param>
    /// <param name="iconSource">The pack or file URI of the icon resource.</param>
    /// <param name="logger">The structured logger instance.</param>
    public TrayIconHost(
        ITrayIcon trayIcon,
        TrayIconViewModel viewModel,
        Action<Action> dispatch,
        Uri iconSource,
        ILogger logger)
    {

        ArgumentNullException.ThrowIfNull(trayIcon);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(iconSource);
        ArgumentNullException.ThrowIfNull(logger);

        _trayIcon = trayIcon;
        _viewModel = viewModel;
        _dispatch = dispatch;
        _iconSource = iconSource;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the notification-area icon failed to initialize and degraded gracefully.
    /// </summary>
    public bool IsDegraded { get; private set; }

    /// <summary>
    /// Initializes and displays the notification-area icon. Safe against repeated calls; catches any creation
    /// exceptions, records a warning, and sets <see cref="IsDegraded"/> rather than throwing.
    /// </summary>
    public void Initialize()
    {

        if (_initialized || _disposed)
            return;

        _initialized = true;

        try
        {

            _trayIcon.Show(
                _iconSource,
                TrayMenuModel.TOOLTIP,
                _viewModel.BuildMenu()
            );

            _logger.Information(
                "Tray icon {State} with tooltip {Tooltip}",
                nameof(Initialize),
                TrayMenuModel.TOOLTIP
            );
        }
        catch (Exception ex)
        {

            _logger.Warning(ex, "Tray icon creation failed; degrading gracefully");
            IsDegraded = true;

            return;
        }

        _trayIcon.LeftClicked += OnLeftClicked;
        _trayIcon.DoubleClicked += OnDoubleClicked;
        _trayIcon.MenuItemInvoked += OnMenuItemInvoked;
        _trayIcon.MenuOpening += OnMenuOpening;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;

        if (_initialized && !IsDegraded)
        {

            _trayIcon.LeftClicked -= OnLeftClicked;
            _trayIcon.DoubleClicked -= OnDoubleClicked;
            _trayIcon.MenuItemInvoked -= OnMenuItemInvoked;
            _trayIcon.MenuOpening -= OnMenuOpening;
        }

        _trayIcon.Dispose();
        _logger.Information("Tray icon {State}", nameof(Dispose));
    }

    private void OnLeftClicked(object? sender, EventArgs e)
    {

        _logger.Information("Tray action {Action} dispatched", nameof(TrayMenuItemKey.ToggleNotch));
        _dispatch(() => _viewModel.ToggleNotch());
    }

    private void OnDoubleClicked(object? sender, EventArgs e)
    {

        _logger.Information("Tray action {Action} dispatched", nameof(TrayMenuItemKey.ToggleNotch));
        _dispatch(() => _viewModel.ToggleNotch());
    }

    private void OnMenuItemInvoked(object? sender, TrayMenuItemKey key)
    {

        _logger.Information("Tray action {Action} dispatched", key);
        _dispatch(() => _viewModel.Invoke(key));
    }

    private void OnMenuOpening(object? sender, EventArgs e)
        => _dispatch(() => _trayIcon.UpdateToggleHeader(_viewModel.ToggleHeader));
}
