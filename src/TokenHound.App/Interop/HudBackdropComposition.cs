using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TokenHound.App.Interop;

/// <summary>
/// Hosts one host-backdrop sprite in a window through hand-written WinRT ABI calls on Windows.UI.Composition,
/// avoiding the Windows SDK projection (TechSpec DEC-08). Slots and IIDs come from <c>Windows.UI.winmd</c>.
/// </summary>
internal sealed class HudBackdropComposition : IDisposable
{

    private const string COMPOSITOR_CLASS = "Windows.UI.Composition.Compositor";
    private const int SLOT_CREATE_SPRITE_VISUAL = 22;
    private const int SLOT_CREATE_HOST_BACKDROP_BRUSH = 6;
    private const int SLOT_PUT_BRUSH = 7;
    private const int SLOT_PUT_SIZE = 36;
    private const int SLOT_PUT_ROOT = 7;
    private const int SLOT_CREATE_DESKTOP_WINDOW_TARGET = 3;
    private const int DQTYPE_THREAD_CURRENT = 2;
    private const int DQTAT_COM_STA = 2;
    private static readonly Guid ICOMPOSITOR = new("b403ca50-7f8c-4e83-985f-cc45060036d8");
    private static readonly Guid ICOMPOSITOR3 = new("c9dd8ef0-6eb1-4e3c-a658-675d9c64d4ab");
    private static readonly Guid ICOMPOSITION_BRUSH = new("ab0d7608-30c0-40e9-b568-b60a6bd1fb46");
    private static readonly Guid IVISUAL = new("117e202d-a859-4c89-873b-c2aa566788e3");
    private static readonly Guid ICOMPOSITION_TARGET = new("a1bea8ba-d726-4663-8129-6b5e7927ffa6");
    private static readonly Guid ICOMPOSITOR_DESKTOP_INTEROP = new("29e691fa-4567-4dca-b319-d0f207eb6807");
    private static IntPtr _dispatcherQueueController;
    private IntPtr _compositor;
    private IntPtr _visual;
    private IntPtr _target;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int OutCall(IntPtr self, out IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int InCall(IntPtr self, IntPtr argument);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SizeCall(IntPtr self, Vector2 size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int TargetCall(IntPtr self, IntPtr window, int isTopmost, out IntPtr target);

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {

        internal int Size;
        internal int ThreadType;
        internal int ApartmentType;
    }

    private HudBackdropComposition()
    {
    }

    /// <summary>Creates the compositor, the host-backdrop sprite, and the desktop window target for a window.</summary>
    /// <param name="window">The companion window; it must use <c>WS_EX_NOREDIRECTIONBITMAP</c>.</param>
    /// <returns>The composition, which owns its native references.</returns>
    internal static HudBackdropComposition Create(IntPtr window)
    {

        EnsureDispatcherQueue();
        var composition = new HudBackdropComposition();

        try
        {

            composition.Build(window);

            return composition;
        }
        catch
        {

            composition.Dispose();

            throw;
        }
    }

    /// <summary>Sizes the sprite to the window, in physical pixels.</summary>
    internal void Resize(int width, int height)
        => Check(Method<SizeCall>(_visual, SLOT_PUT_SIZE)(_visual, new Vector2(width, height)));

    /// <inheritdoc />
    public void Dispose()
    {

        Release(ref _target);
        Release(ref _visual);
        Release(ref _compositor);
    }

    private void Build(IntPtr window)
    {

        _compositor = Activate();
        _visual = CreateBackdropVisual();
        var interop = Query(_compositor, ICOMPOSITOR_DESKTOP_INTEROP);

        try
        {

            Check(Method<TargetCall>(interop, SLOT_CREATE_DESKTOP_WINDOW_TARGET)(interop, window, 0, out _target));
            SetRoot();
        }
        finally
        {

            Marshal.Release(interop);
        }
    }

    private IntPtr CreateBackdropVisual()
    {

        var compositor = Query(_compositor, ICOMPOSITOR);
        var compositor3 = Query(_compositor, ICOMPOSITOR3);
        IntPtr sprite = IntPtr.Zero, backdrop = IntPtr.Zero, brush = IntPtr.Zero;

        try
        {

            Check(Method<OutCall>(compositor, SLOT_CREATE_SPRITE_VISUAL)(compositor, out sprite));
            Check(Method<OutCall>(compositor3, SLOT_CREATE_HOST_BACKDROP_BRUSH)(compositor3, out backdrop));
            brush = Query(backdrop, ICOMPOSITION_BRUSH);
            Check(Method<InCall>(sprite, SLOT_PUT_BRUSH)(sprite, brush));

            return Query(sprite, IVISUAL);
        }
        finally
        {

            Release(ref brush);
            Release(ref backdrop);
            Release(ref sprite);
            Marshal.Release(compositor3);
            Marshal.Release(compositor);
        }
    }

    private void SetRoot()
    {

        var target = Query(_target, ICOMPOSITION_TARGET);

        try
        {

            Check(Method<InCall>(target, SLOT_PUT_ROOT)(target, _visual));
        }
        finally
        {

            Marshal.Release(target);
        }
    }

    private static IntPtr Activate()
    {

        Check(WindowsCreateString(COMPOSITOR_CLASS, COMPOSITOR_CLASS.Length, out var className));

        try
        {

            Check(RoActivateInstance(className, out var instance));

            return instance;
        }
        finally
        {

            WindowsDeleteString(className);
        }
    }

    private static void EnsureDispatcherQueue()
    {

        if (_dispatcherQueueController != IntPtr.Zero)
            return;

        var options = new DispatcherQueueOptions
        {
            Size = Marshal.SizeOf<DispatcherQueueOptions>(),
            ThreadType = DQTYPE_THREAD_CURRENT,
            ApartmentType = DQTAT_COM_STA
        };
        Check(CreateDispatcherQueueController(options, out _dispatcherQueueController));
    }

    private static T Method<T>(IntPtr instance, int slot)
        where T : Delegate
        => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));

    private static IntPtr Query(IntPtr instance, Guid iid)
    {

        Check(Marshal.QueryInterface(instance, in iid, out var result));

        return result;
    }

    private static void Release(ref IntPtr instance)
    {

        if (instance == IntPtr.Zero)
            return;

        Marshal.Release(instance);
        instance = IntPtr.Zero;
    }

    private static void Check(int result)
        => Marshal.ThrowExceptionForHR(result);

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr controller);

    [DllImport("combase.dll", CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string source, int length, out IntPtr value);

    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(IntPtr value);

    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(IntPtr classId, out IntPtr instance);
}
