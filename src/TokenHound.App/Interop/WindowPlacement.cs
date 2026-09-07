using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace TokenHound.App.Interop;

/// <summary>
/// Provides interop helpers for monitor work-area detection and dialog window placement.
/// </summary>
public static class WindowPlacement
{
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const double DEFAULT_DIALOG_WIDTH = 440.0;
    private const double DEFAULT_DIALOG_HEIGHT = 280.0;

    /// <summary>
    /// Retrieves the desktop work area in WPF device-independent units for the monitor hosting the specified window handle.
    /// </summary>
    /// <param name="hwnd">The native window handle used to identify the target monitor.</param>
    /// <param name="dpi">The DPI scaling factors for unit conversion.</param>
    /// <returns>The work area bounding rectangle in WPF units.</returns>
    public static Rect GetWorkArea(IntPtr hwnd, DpiScale dpi)
    {

        if (OperatingSystem.IsWindows() && hwnd != IntPtr.Zero)
        {
            var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (hMonitor != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    var scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                    var scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                    var left = mi.rcWork.Left / scaleX;
                    var top = mi.rcWork.Top / scaleY;
                    var width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
                    var height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;

                    return new Rect(
                        left,
                        top,
                        width,
                        height
                    );
                }
            }
        }

        return SystemParameters.WorkArea;
    }

    /// <summary>
    /// Positions the specified dialog window centered within the work area of the monitor hosting its owner.
    /// </summary>
    /// <param name="window">The dialog window to position.</param>
    /// <param name="ownerWindow">The optional owner window identifying the target monitor.</param>
    public static void PositionInWorkArea(Window window, Window? ownerWindow)
    {

        var ownerHwnd = ownerWindow is not null
            ? new WindowInteropHelper(ownerWindow).Handle
            : IntPtr.Zero;

        var dpi = VisualTreeHelper.GetDpi(ownerWindow ?? window);
        var workArea = GetWorkArea(ownerHwnd, dpi);

        window.Measure(new Size(workArea.Width, workArea.Height));
        var width = double.IsNaN(window.Width) ? window.DesiredSize.Width : window.Width;
        var height = double.IsNaN(window.Height) ? window.DesiredSize.Height : window.Height;

        if (width <= 0)
            width = DEFAULT_DIALOG_WIDTH;

        if (height <= 0)
            height = DEFAULT_DIALOG_HEIGHT;

        var left = workArea.Left + ((workArea.Width - width) / 2.0);
        var top = workArea.Top + ((workArea.Height - height) / 2.0);

        left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - width));
        top = Math.Max(workArea.Top, Math.Min(top, workArea.Bottom - height));

        window.Left = left;
        window.Top = top;
    }

    /// <summary>
    /// Enables dark mode on the native title bar for the specified window handle when running on Windows.
    /// </summary>
    /// <param name="hwnd">The native window handle.</param>
    public static void EnableDarkMode(IntPtr hwnd)
    {

        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
            return;

        var darkMode = 1;

        DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref darkMode,
            sizeof(int)
        );
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize
    );

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}
