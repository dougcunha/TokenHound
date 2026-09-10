namespace TokenHound.App.UI.Tray;

/// <summary>
/// Boundary over the OS notification-area icon; no live <c>NotifyIcon</c> instance leaks past this type.
/// </summary>
public interface ITrayIcon : IDisposable
{
    /// <summary>
    /// Registers the notification-area icon with its image, hover tooltip, and context menu.
    /// </summary>
    /// <param name="iconSource">The pack or file URI of the icon image to display.</param>
    /// <param name="tooltip">The hover tooltip text shown for the icon.</param>
    /// <param name="menu">The descriptor listing the context-menu entries in their fixed order.</param>
    void Show(Uri iconSource, string tooltip, TrayMenuDescriptor menu);

    /// <summary>
    /// Retargets the header text of the visibility-toggle context-menu entry.
    /// </summary>
    /// <param name="header">The new header text for the toggle entry.</param>
    void UpdateToggleHeader(string header);

    /// <summary>
    /// Occurs when the user performs a single left-click on the icon.
    /// </summary>
    event EventHandler LeftClicked;

    /// <summary>
    /// Occurs when the user performs a double-click on the icon.
    /// </summary>
    event EventHandler DoubleClicked;

    /// <summary>
    /// Occurs immediately before the context menu is shown, so the caller can refresh its state.
    /// </summary>
    event EventHandler MenuOpening;

    /// <summary>
    /// Occurs when the user invokes a context-menu entry; the argument carries the invoked entry's key.
    /// </summary>
    event EventHandler<TrayMenuItemKey> MenuItemInvoked;
}
