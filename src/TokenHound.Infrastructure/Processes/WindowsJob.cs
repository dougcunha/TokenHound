using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TokenHound.Infrastructure.Processes;

[SupportedOSPlatform("windows")]
internal sealed class WindowsJob : SafeHandleZeroOrMinusOneIsInvalid
{
    private const uint KILL_ON_JOB_CLOSE = 0x2000;
    private const int EXTENDED_LIMIT_INFORMATION = 9;

    private WindowsJob() : base(true)
    {
    }

    internal static WindowsJob Create()
    {

        var job = CreateJobObjectW(IntPtr.Zero, null);

        try
        {

            if (job.IsInvalid)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            var limits = new ExtendedLimits { Basic = new BasicLimits { Flags = KILL_ON_JOB_CLOSE } };

            if (!SetInformationJobObject(
                job,
                EXTENDED_LIMIT_INFORMATION,
                ref limits,
                Marshal.SizeOf<ExtendedLimits>()
            ))
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            return job;
        }
        catch
        {

            job.Dispose();
            throw;
        }
    }

    protected override bool ReleaseHandle()
        => CloseHandle(handle);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern WindowsJob CreateJobObjectW(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(
        WindowsJob job,
        int informationClass,
        ref ExtendedLimits information,
        int length);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long ProcessTime;
        public long JobTime;
        public uint Flags;
        public UIntPtr MinimumWorkingSet;
        public UIntPtr MaximumWorkingSet;
        public uint ActiveProcesses;
        public UIntPtr Affinity;
        public uint Priority;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public ulong ReadOperations;
        public ulong WriteOperations;
        public ulong OtherOperations;
        public ulong ReadBytes;
        public ulong WriteBytes;
        public ulong OtherBytes;
        public UIntPtr ProcessMemory;
        public UIntPtr JobMemory;
        public UIntPtr PeakProcessMemory;
        public UIntPtr PeakJobMemory;
    }
}
