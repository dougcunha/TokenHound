using System;
using System.Runtime.InteropServices;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.Interop;

/// <summary>
/// Reads and rewrites the proposed window rectangle that <c>WM_MOVING</c> passes in its <c>lParam</c>.
/// </summary>
public static class MovingRect
{
    /// <summary>
    /// Reads the proposed window rectangle in physical pixels.
    /// </summary>
    /// <param name="lParam">The <c>WM_MOVING</c> parameter that points to the rectangle.</param>
    /// <returns>The proposed window bounds.</returns>
    public static ScreenBounds Read(IntPtr lParam)
    {

        var rect = Marshal.PtrToStructure<RECT>(lParam);

        return new ScreenBounds
        {
            Left = rect.Left,
            Top = rect.Top,
            Width = rect.Right - rect.Left,
            Height = rect.Bottom - rect.Top
        };
    }

    /// <summary>
    /// Moves the proposed window rectangle to the specified offsets and keeps its size.
    /// </summary>
    /// <param name="lParam">The <c>WM_MOVING</c> parameter that points to the rectangle.</param>
    /// <param name="bounds">The proposed window bounds that supply the size.</param>
    /// <param name="left">The new left edge in physical pixels.</param>
    /// <param name="top">The new top edge in physical pixels.</param>
    public static void Write(IntPtr lParam, ScreenBounds bounds, int left, int top)
    {

        ArgumentNullException.ThrowIfNull(bounds);

        var rect = new RECT
        {
            Left = left,
            Top = top,
            Right = left + (int)bounds.Width,
            Bottom = top + (int)bounds.Height
        };

        Marshal.StructureToPtr(rect, lParam, false);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
