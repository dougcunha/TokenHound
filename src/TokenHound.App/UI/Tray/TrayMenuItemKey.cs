namespace TokenHound.App.UI.Tray;

/// <summary>
/// Identifies a tray context-menu entry. The declaration order is load-bearing and matches the rendered menu layout.
/// </summary>
public enum TrayMenuItemKey
{
    /// <summary>The Notch visibility toggle entry ("Hide Notch" / "Show Notch").</summary>
    ToggleNotch,

    /// <summary>The immediate provider-refresh entry ("Refresh Now").</summary>
    RefreshNow,

    /// <summary>The entry that opens the modeless Settings dialog.</summary>
    Settings,

    /// <summary>The entry that opens the modeless About dialog.</summary>
    About,

    /// <summary>The entry that triggers the single coordinated application shutdown.</summary>
    Exit
}
