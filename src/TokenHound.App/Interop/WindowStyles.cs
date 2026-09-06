using System;
using System.Runtime.InteropServices;

namespace TokenHound.App.Interop;

/// <summary>
/// Provides Win32 extended window style constants and interop helpers for non-activating floating windows.
/// </summary>
public static class WindowStyles
{
    /// <summary>
    /// Window field index to retrieve or set extended window styles.
    /// </summary>
    public const int GWL_EXSTYLE = -20;

    /// <summary>
    /// Extended window style: window does not become the foreground window when clicked.
    /// </summary>
    public const int WS_EX_NOACTIVATE = 0x08000000;

    /// <summary>
    /// Extended window style: floating toolbar that does not appear in the taskbar or Alt+Tab dialog.
    /// </summary>
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    /// <summary>
    /// Extended window style: window placed above all non-topmost windows and stays above them.
    /// </summary>
    public const int WS_EX_TOPMOST = 0x00000008;

    /// <summary>
    /// Applies non-activating extended styles (<see cref="WS_EX_NOACTIVATE"/>, <see cref="WS_EX_TOOLWINDOW"/>, and <see cref="WS_EX_TOPMOST"/>)
    /// to the specified current style bitmask without removing existing styles.
    /// </summary>
    /// <param name="currentStyle">The current window extended styles bitmask.</param>
    /// <returns>The updated style bitmask containing all non-activating flags.</returns>
    public static int ApplyExtendedStyles(int currentStyle)
        => currentStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;

    /// <summary>
    /// Determines whether the specified style bitmask contains all non-activating flags.
    /// </summary>
    /// <param name="style">The window extended styles bitmask to evaluate.</param>
    /// <returns><see langword="true"/> if all required flags are present; otherwise, <see langword="false"/>.</returns>
    public static bool HasNonActivatingStyles(int style)
        => (style & WS_EX_NOACTIVATE) == WS_EX_NOACTIVATE
            && (style & WS_EX_TOOLWINDOW) == WS_EX_TOOLWINDOW
            && (style & WS_EX_TOPMOST) == WS_EX_TOPMOST;

    /// <summary>
    /// Enables non-activating extended window styles on the specified native window handle.
    /// </summary>
    /// <param name="hwnd">The native window handle.</param>
    public static void EnableNonActivating(IntPtr hwnd)
    {

        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
            return;

        var currentStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        var newStyle = ApplyExtendedStyles(currentStyle);

        if (newStyle != currentStyle)
            SetWindowLongW(hwnd, GWL_EXSTYLE, newStyle);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLongW(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);
}
