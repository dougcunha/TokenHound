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
    private const long MAX_USER_ADDRESS = 0x7FFFFFFF0000L;

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
        string? fallbackToken = null;

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
                var goStructToken = ScanProcessRegion(
                    processHandle,
                    mbi.BaseAddress,
                    mbi.RegionSize.ToInt64(),
                    buffer,
                    ref fallbackToken
                );

                if (!string.IsNullOrEmpty(goStructToken))
                    return goStructToken;
            }

            currentAddress = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();
        }

        return fallbackToken;
    }

    private static bool IsReadableCommittedRegion(MEMORY_BASIC_INFORMATION mbi)
        => mbi.State == MEM_COMMIT && (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_EXECUTE_READWRITE);

    private static string? ScanProcessRegion(
        IntPtr processHandle,
        IntPtr baseAddress,
        long regionSize,
        byte[] buffer,
        ref string? fallbackToken)
        => ScanRegionCore(
            (ProcessHandle: processHandle, BaseAddress: baseAddress.ToInt64()),
            regionSize,
            buffer,
            static (state, offset, chunkBuffer, readSize) => ReadProcessAt(
                state.ProcessHandle,
                state.BaseAddress + offset,
                chunkBuffer,
                readSize
            ),
            static (state, address, scratchBuffer, readSize) => ReadProcessAt(
                state.ProcessHandle,
                address,
                scratchBuffer,
                readSize
            ),
            ref fallbackToken
        );

    internal static string? ScanRegion<TState>(
        TState state,
        long regionSize,
        byte[] buffer,
        Func<TState, long, byte[], int, int> readChunk,
        Func<TState, long, byte[], int, int>? readAtAddress = null)
    {
        string? fallbackToken = null;
        var goStructToken = ScanRegionCore(
            state,
            regionSize,
            buffer,
            readChunk,
            readAtAddress,
            ref fallbackToken
        );

        return goStructToken ?? fallbackToken;
    }

    private static string? ScanRegionCore<TState>(
        TState state,
        long regionSize,
        byte[] buffer,
        Func<TState, long, byte[], int, int> readChunk,
        Func<TState, long, byte[], int, int>? readAtAddress,
        ref string? fallbackToken)
    {
        var step = Math.Max(buffer.Length - AntigravityCsrfTokenScanner.CHUNK_OVERLAP, 1);
        var scratch = readAtAddress is not null
            ? new byte[AntigravityCsrfTokenScanner.SCRATCH_BUFFER_SIZE]
            : [];

        for (var regionOffset = 0L; regionOffset < regionSize; regionOffset += step)
        {
            var readSize = (int)Math.Min(regionSize - regionOffset, buffer.Length);
            var bytesRead = readChunk(
                state,
                regionOffset,
                buffer,
                readSize
            );
            var goStructToken = ScanChunk(
                state,
                buffer.AsSpan(0, Math.Max(bytesRead, 0)),
                scratch,
                readAtAddress,
                ref fallbackToken
            );

            if (!string.IsNullOrEmpty(goStructToken))
                return goStructToken;

            if (regionOffset + readSize >= regionSize)
                break;
        }

        return null;
    }

    private static string? ScanChunk<TState>(
        TState state,
        ReadOnlySpan<byte> chunk,
        byte[] scratch,
        Func<TState, long, byte[], int, int>? readAtAddress,
        ref string? fallbackToken)
    {

        if (chunk.IsEmpty)
            return null;

        if (readAtAddress is not null)
        {
            var goStructToken = AntigravityCsrfTokenScanner.FindGoStructToken(
                chunk,
                state,
                scratch,
                readAtAddress
            );

            if (!string.IsNullOrEmpty(goStructToken))
                return goStructToken;
        }

        fallbackToken ??= AntigravityCsrfTokenScanner.FindPrefixToken(chunk);

        return null;
    }

    private static int ReadProcessAt(
        IntPtr processHandle,
        long address,
        byte[] buffer,
        int readSize)
    {
        var readSuccess = ReadProcessMemory(
            processHandle,
            (IntPtr)address,
            buffer,
            (IntPtr)readSize,
            out var bytesRead
        );

        return readSuccess ? (int)bytesRead : 0;
    }

    internal static string? FindTokenInSpan(ReadOnlySpan<byte> span)
        => AntigravityCsrfTokenScanner.FindPrefixToken(span);

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
