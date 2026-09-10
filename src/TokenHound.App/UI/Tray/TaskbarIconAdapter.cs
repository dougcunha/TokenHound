using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TokenHound.App.UI.Tray;

/// <summary>
/// Adapts the WPF <see cref="TaskbarIcon"/> notification-area icon to the <see cref="ITrayIcon"/> contract.
/// </summary>
public sealed class TaskbarIconAdapter : ITrayIcon
{
    private readonly TaskbarIcon _taskbarIcon;

    private MenuItem? _toggleMenuItem;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskbarIconAdapter"/> class.
    /// </summary>
    public TaskbarIconAdapter()
    {

        _taskbarIcon = new TaskbarIcon();
        _taskbarIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;
        _taskbarIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
        _taskbarIcon.TrayContextMenuOpen += OnTrayContextMenuOpen;
    }

    /// <inheritdoc />
    public event EventHandler? LeftClicked;

    /// <inheritdoc />
    public event EventHandler? DoubleClicked;

    /// <inheritdoc />
    public event EventHandler? MenuOpening;

    /// <inheritdoc />
    public event EventHandler<TrayMenuItemKey>? MenuItemInvoked;

    /// <inheritdoc />
    public void Show(Uri iconSource, string tooltip, TrayMenuDescriptor menu)
    {

        ArgumentNullException.ThrowIfNull(iconSource);
        ArgumentNullException.ThrowIfNull(tooltip);
        ArgumentNullException.ThrowIfNull(menu);

        _taskbarIcon.IconSource = new BitmapImage(iconSource);
        _taskbarIcon.ToolTipText = tooltip;

        var menuStyle = Application.Current?.TryFindResource("HudContextMenuStyle") as Style;
        var itemStyle = Application.Current?.TryFindResource("HudMenuItemStyle") as Style;

        _taskbarIcon.ContextMenu = BuildContextMenu(menu, menuStyle, itemStyle);
    }

    /// <inheritdoc />
    public void UpdateToggleHeader(string header)
    {

        ArgumentNullException.ThrowIfNull(header);

        if (_toggleMenuItem is not null)
            _toggleMenuItem.Header = header;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;

        _taskbarIcon.TrayLeftMouseUp -= OnTrayLeftMouseUp;
        _taskbarIcon.TrayMouseDoubleClick -= OnTrayMouseDoubleClick;
        _taskbarIcon.TrayContextMenuOpen -= OnTrayContextMenuOpen;

        _taskbarIcon.Dispose();
    }

    private ContextMenu BuildContextMenu(
        TrayMenuDescriptor menu,
        Style? menuStyle,
        Style? itemStyle)
    {

        var contextMenu = new ContextMenu();
        if (menuStyle is not null)
            contextMenu.Style = menuStyle;

        foreach (var entry in menu.Entries)
        {

            if (entry.PrecededBySeparator)
                contextMenu.Items.Add(new Separator());

            var menuItem = CreateMenuItem(entry, itemStyle);
            if (entry.Key == TrayMenuItemKey.ToggleNotch)
                _toggleMenuItem = menuItem;

            contextMenu.Items.Add(menuItem);
        }

        return contextMenu;
    }

    private MenuItem CreateMenuItem(TrayMenuEntry entry, Style? itemStyle)
    {

        var menuItem = new MenuItem
        {
            Header = entry.Header
        };

        if (itemStyle is not null)
            menuItem.Style = itemStyle;

        var key = entry.Key;
        menuItem.Click += (_, _) => MenuItemInvoked?.Invoke(this, key);

        return menuItem;
    }

    private void OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
        => LeftClicked?.Invoke(this, EventArgs.Empty);

    private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        => DoubleClicked?.Invoke(this, EventArgs.Empty);

    private void OnTrayContextMenuOpen(object sender, RoutedEventArgs e)
        => MenuOpening?.Invoke(this, EventArgs.Empty);
}
