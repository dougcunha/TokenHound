using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TokenHound.App.Interop;

/// <summary>Win32 calls for the raw, input-transparent HUD backdrop companion window.</summary>
internal static class HudBackdropInterop
{

    internal const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;

    /// <summary>Extended styles of the companion: passes input, never activates, content from composition only.</summary>
    internal const int EXTENDED_STYLE = WindowStyles.WS_EX_TRANSPARENT
        | WindowStyles.WS_EX_NOACTIVATE
        | WindowStyles.WS_EX_TOOLWINDOW
        | WindowStyles.WS_EX_TOPMOST
        | WS_EX_NOREDIRECTIONBITMAP;

    private const string CLASS_NAME = "TokenHoundHudBackdrop";
    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int WM_NCHITTEST = 0x0084;
    private const int MA_NOACTIVATE = 3;
    private const int HTTRANSPARENT = -1;
    private const int SW_HIDE = 0;
    private const int WINDING = 2;
    private const int DWMWA_USE_HOSTBACKDROPBRUSH = 17;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private static readonly WindowProcedure PROCEDURE = WndProc;
    private static bool _registered;

    private delegate IntPtr WindowProcedure(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {

        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {

        internal uint Size;
        internal uint Style;
        internal IntPtr Procedure;
        internal int ClassExtra;
        internal int WindowExtra;
        internal IntPtr Instance;
        internal IntPtr Icon;
        internal IntPtr Cursor;
        internal IntPtr Background;
        internal string? MenuName;
        internal string ClassName;
        internal IntPtr SmallIcon;
    }

    /// <summary>Creates the hidden companion and opts it into the host backdrop brush.</summary>
    internal static IntPtr Create()
    {

        Register();
        var handle = CreateWindowExW(
            EXTENDED_STYLE,
            CLASS_NAME,
            string.Empty,
            WS_POPUP,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandleW(null),
            IntPtr.Zero
        );

        if (handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(CreateWindowExW));

        EnableHostBackdrop(handle);

        return handle;
    }

    /// <summary>Shows the companion at the HUD bounds, directly below the HUD in z-order, without activating it.</summary>
    internal static void Place(IntPtr handle, IntPtr hud, HudShadowInterop.Bounds bounds)
    {

        var success = SetWindowPos(
            handle,
            hud,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW
        );

        if (!success)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(SetWindowPos));
    }

    /// <summary>Clips the companion to a polygon in window pixels; the system takes ownership of the region.</summary>
    internal static void SetRegion(IntPtr handle, NativePoint[] points)
    {

        var region = CreatePolygonRgn(points, points.Length, WINDING);

        if (region == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(CreatePolygonRgn));

        if (SetWindowRgn(handle, region, true) != 0)
            return;

        DeleteObject(region);

        throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(SetWindowRgn));
    }

    internal static void Hide(IntPtr handle)
        => ShowWindow(handle, SW_HIDE);

    internal static void Destroy(IntPtr handle)
        => DestroyWindow(handle);

    private static void Register()
    {

        if (_registered)
            return;

        var windowClass = new WindowClass
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            Procedure = Marshal.GetFunctionPointerForDelegate(PROCEDURE),
            Instance = GetModuleHandleW(null),
            ClassName = CLASS_NAME
        };

        if (RegisterClassExW(ref windowClass) == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(RegisterClassExW));

        _registered = true;
    }

    private static void EnableHostBackdrop(IntPtr handle)
    {

        var enabled = 1;
        var result = DwmSetWindowAttribute(
            handle,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            ref enabled,
            sizeof(int)
        );

        if (result >= 0)
            return;

        DestroyWindow(handle);
        Marshal.ThrowExceptionForHR(result);
    }

    private static IntPtr WndProc(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
        => message switch
        {
            WM_MOUSEACTIVATE => new IntPtr(MA_NOACTIVATE),
            WM_NCHITTEST => new IntPtr(HTTRANSPARENT),
            _ => DefWindowProcW(
                handle,
                message,
                wParam,
                lParam
            )
        };

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        int extendedStyle,
        string className,
        string windowName,
        int style,
        int left,
        int top,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr parameter
    );

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr handle,
        IntPtr insertAfter,
        int left,
        int top,
        int width,
        int height,
        uint flags
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowRgn(IntPtr handle, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr handle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr handle);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreatePolygonRgn(NativePoint[] points, int count, int fillMode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? name);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}
