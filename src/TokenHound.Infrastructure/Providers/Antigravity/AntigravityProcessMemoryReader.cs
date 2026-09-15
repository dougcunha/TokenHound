using System;
using System.Runtime.InteropServices;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Scans process memory on Windows to resolve the Antigravity CSRF token.
/// </summary>
internal static class AntigravityProcessMemoryReader
{
    private const int PROCESS_QUERY_INFORMATION = 0x0400;
    private const int PROCESS_VM_READ = 0x0010;
    private const uint MEM_COMMIT = 0x1000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const int MAX_REGION_READ_SIZE = 1048576;
    private const int GUID_STRING_LENGTH = 36;
    private const long MAX_USER_ADDRESS = 0x7FFFFFFF0000L;

    private static readonly byte[] TARGET_PREFIX = "ANTIGRAVITY_CSRF_TOKEN="u8.ToArray();

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public ushort PartitionId;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    /// <summary>
    /// Searches the committed memory of a process for an Antigravity CSRF token.
    /// </summary>
    /// <param name="processId">The target process identifier.</param>
    /// <returns>The resolved token string, or null if not found.</returns>
    public static string? FindCsrfToken(int processId)
    {

        if (!OperatingSystem.IsWindows() || processId <= 0)
            return null;

        var processHandle = OpenProcess(
            PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
            false,
            processId
        );

        if (processHandle == IntPtr.Zero)
            return null;

        try
        {
            return ScanProcessCommittedMemory(processHandle);
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private static string? ScanProcessCommittedMemory(IntPtr processHandle)
    {
        var currentAddress = 0L;
        var buffer = new byte[MAX_REGION_READ_SIZE];

        while (currentAddress < MAX_USER_ADDRESS)
        {

            var queryResult = VirtualQueryEx(
                processHandle,
                (IntPtr)currentAddress,
                out var mbi,
                (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()
            );

            if (queryResult == IntPtr.Zero)
                break;

            if (IsReadableCommittedRegion(mbi))
            {
                var token = TryReadAndFindToken(
                    processHandle,
                    mbi.BaseAddress,
                    mbi.RegionSize.ToInt64(),
                    buffer
                );

                if (!string.IsNullOrEmpty(token))
                    return token;
            }

            currentAddress = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();
        }

        return null;
    }

    private static bool IsReadableCommittedRegion(MEMORY_BASIC_INFORMATION mbi)
        => mbi.State == MEM_COMMIT && (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_EXECUTE_READWRITE);

    private static string? TryReadAndFindToken(
        IntPtr processHandle,
        IntPtr baseAddress,
        long regionSize,
        byte[] buffer)
    {
        var readSize = (int)Math.Min(regionSize, MAX_REGION_READ_SIZE);

        var readSuccess = ReadProcessMemory(
            processHandle,
            baseAddress,
            buffer,
            (IntPtr)readSize,
            out var bytesRead
        );

        if (!readSuccess)
            return null;

        var span = new ReadOnlySpan<byte>(buffer, 0, (int)bytesRead);
        var offset = 0;

        while (offset < span.Length)
        {
            var index = span[offset..].IndexOf(TARGET_PREFIX);

            if (index < 0)
                break;

            var matchPos = offset + index;
            var tokenStart = matchPos + TARGET_PREFIX.Length;

            if (tokenStart + GUID_STRING_LENGTH <= span.Length)
            {
                var token = ExtractValidToken(span.Slice(tokenStart, GUID_STRING_LENGTH));

                if (!string.IsNullOrEmpty(token))
                    return token;
            }

            offset = matchPos + TARGET_PREFIX.Length;
        }

        return null;
    }

    private static string? ExtractValidToken(ReadOnlySpan<byte> tokenBytes)
    {
        Span<char> charBuffer = stackalloc char[GUID_STRING_LENGTH];

        for (var i = 0; i < GUID_STRING_LENGTH; i++)
        {
            charBuffer[i] = (char)tokenBytes[i];
        }

        if (Guid.TryParse(charBuffer, out var guid))
            return guid.ToString();

        return null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        int processAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualQueryEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        uint dwLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        IntPtr dwSize,
        out IntPtr lpNumberOfBytesRead);
}
