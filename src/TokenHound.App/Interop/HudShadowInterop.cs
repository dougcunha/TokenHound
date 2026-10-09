using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TokenHound.App.Interop;

internal static class HudShadowInterop
{

    private const uint SWP_FLAGS = 0x0010 | 0x0004;

    internal readonly record struct Bounds(int Left, int Top, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {

        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    internal static void EnablePassive(IntPtr handle)
    {

        Marshal.SetLastPInvokeError(0);
        var current = GetWindowLongW(handle, WindowStyles.GWL_EXSTYLE);
        var error = Marshal.GetLastPInvokeError();

        if (current == 0 && error != 0)
            throw new Win32Exception(error, nameof(GetWindowLongW));

        Marshal.SetLastPInvokeError(0);
        var previous = SetWindowLongW(handle, WindowStyles.GWL_EXSTYLE, WindowStyles.ApplyShadowStyles(current));
        error = Marshal.GetLastPInvokeError();

        if (previous == 0 && error != 0)
            throw new Win32Exception(error, nameof(SetWindowLongW));
    }

    internal static Bounds GetBounds(IntPtr handle)
    {

        if (!GetWindowRect(handle, out var rectangle))
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(GetWindowRect));

        return new Bounds(
            rectangle.Left,
            rectangle.Top,
            rectangle.Right - rectangle.Left,
            rectangle.Bottom - rectangle.Top
        );
    }

    internal static void SetBounds(IntPtr handle, Bounds bounds)
    {

        var success = SetWindowPos(
            handle,
            IntPtr.Zero,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            SWP_FLAGS
        );

        if (!success)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), nameof(SetWindowPos));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLongW(IntPtr handle, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLongW(IntPtr handle, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rectangle);

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
}
