using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TokenHound.Infrastructure.Processes;

[SupportedOSPlatform("windows")]
internal sealed class WindowsProcessAttributes : SafeHandleZeroOrMinusOneIsInvalid
{
    private const int HANDLE_LIST = 0x20002;
    private const int JOB_LIST = 0x2000D;
    private readonly List<IntPtr> _values = [];
    private bool _initialized;

    internal WindowsProcessAttributes() : base(true)
    {

        nuint size = 0;
        InitializeProcThreadAttributeList(
            IntPtr.Zero,
            2,
            0,
            ref size
        );
        SetHandle(Marshal.AllocHGlobal(checked((int)size)));

        if (!InitializeProcThreadAttributeList(
            handle,
            2,
            0,
            ref size
        ))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        _initialized = true;
    }

    internal void SetHandles(WindowsJob job, IntPtr[] streams)
    {

        Add(JOB_LIST, [job.DangerousGetHandle()]);
        Add(HANDLE_LIST, streams);
    }

    private void Add(int attribute, IntPtr[] values)
    {

        var memory = Marshal.AllocHGlobal(values.Length * IntPtr.Size);
        _values.Add(memory);
        Marshal.Copy(
            values,
            0,
            memory,
            values.Length
        );

        if (!UpdateProcThreadAttribute(
            handle,
            0,
            (nuint)attribute,
            memory,
            (nuint)(values.Length * IntPtr.Size),
            IntPtr.Zero,
            IntPtr.Zero
        ))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    protected override bool ReleaseHandle()
    {

        if (_initialized)
            DeleteProcThreadAttributeList(handle);

        Marshal.FreeHGlobal(handle);

        foreach (var value in _values)
            Marshal.FreeHGlobal(value);

        return true;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr list,
        uint flags,
        nuint attribute,
        IntPtr value,
        nuint size,
        IntPtr previousValue,
        IntPtr returnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr list);
}
