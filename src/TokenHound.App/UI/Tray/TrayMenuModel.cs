namespace TokenHound.App.UI.Tray;

/// <summary>
/// Supplies the tray context-menu copy and builds the ordered entry list for a given Notch visibility state.
/// </summary>
public sealed class TrayMenuModel
{
    /// <summary>Hover tooltip text for the notification-area icon.</summary>
    public const string TOOLTIP = "TokenHound";

    /// <summary>Toggle-entry header shown while the Notch is visible.</summary>
    public const string HIDE_NOTCH_HEADER = "Hide Notch";

    /// <summary>Toggle-entry header shown while the Notch is hidden.</summary>
    public const string SHOW_NOTCH_HEADER = "Show Notch";

    /// <summary>Header for the immediate-refresh entry.</summary>
    public const string REFRESH_HEADER = "Refresh Now";

    /// <summary>Header for the entry that opens the Settings dialog.</summary>
    public const string SETTINGS_HEADER = "Settings\u2026";

    /// <summary>Header for the entry that opens the About dialog.</summary>
    public const string ABOUT_HEADER = "About\u2026";

    /// <summary>Header for the coordinated-shutdown entry.</summary>
    public const string EXIT_HEADER = "Exit";

    /// <summary>
    /// Builds the ordered tray context-menu entries for the supplied Notch visibility state.
    /// </summary>
    /// <param name="notchVisible"><see langword="true"/> when the Notch is currently visible.</param>
    /// <returns>
    /// Exactly five entries in the fixed order ToggleNotch, RefreshNow, Settings, About, Exit, with a
    /// separator preceding <see cref="TrayMenuItemKey.Exit"/> and no separator before any other entry.
    /// </returns>
    public IReadOnlyList<TrayMenuEntry> BuildDescriptor(bool notchVisible)
    {

        return
        [
            new() { Key = TrayMenuItemKey.ToggleNotch, Header = ResolveToggleHeader(notchVisible) },
            new() { Key = TrayMenuItemKey.RefreshNow, Header = REFRESH_HEADER },
            new() { Key = TrayMenuItemKey.Settings, Header = SETTINGS_HEADER },
            new() { Key = TrayMenuItemKey.About, Header = ABOUT_HEADER },
            new() { Key = TrayMenuItemKey.Exit, Header = EXIT_HEADER, PrecededBySeparator = true },
        ];
    }

    /// <summary>
    /// Resolves the visibility-toggle header for the supplied Notch visibility state.
    /// </summary>
    /// <param name="notchVisible"><see langword="true"/> when the Notch is currently visible.</param>
    /// <returns><see cref="HIDE_NOTCH_HEADER"/> when visible; otherwise <see cref="SHOW_NOTCH_HEADER"/>.</returns>
    public static string ResolveToggleHeader(bool notchVisible)
        => notchVisible ? HIDE_NOTCH_HEADER : SHOW_NOTCH_HEADER;
}

/// <summary>
/// A single tray context-menu entry: which action it invokes, its display header, and whether a separator precedes it.
/// </summary>
public sealed record TrayMenuEntry
{
    /// <summary>Gets the key identifying which action this entry invokes.</summary>
    public required TrayMenuItemKey Key { get; init; }

    /// <summary>Gets the display header text for this entry.</summary>
    public required string Header { get; init; }

    /// <summary>Gets a value indicating whether a separator is rendered immediately before this entry.</summary>
    public bool PrecededBySeparator { get; init; }
}

/// <summary>
/// The complete ordered set of tray context-menu entries: ToggleNotch, RefreshNow, Settings, About, then a separator and Exit.
/// </summary>
public sealed record TrayMenuDescriptor
{
    /// <summary>Gets the ordered context-menu entries.</summary>
    public required IReadOnlyList<TrayMenuEntry> Entries { get; init; }
}
