using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Discovers running Google Antigravity language server endpoints on Windows.
/// </summary>
public sealed class AntigravityEndpointDiscovery
{
    private static readonly Regex CsrfTokenRegex = new(@"--csrf_token\s+([^\s]+)", RegexOptions.Compiled);

    private readonly Func<IEnumerable<(int Pid, string CommandLine)>> _processEnumerator;
    private readonly Func<int, IReadOnlyList<int>> _portResolver;

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public uint localPort;
        public uint remoteAddr;
        public uint remotePort;
        public uint owningPid;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityEndpointDiscovery"/> class.
    /// </summary>
    /// <param name="processEnumerator">Optional process enumerator for testing.</param>
    /// <param name="portResolver">Optional port resolver for testing.</param>
    public AntigravityEndpointDiscovery(
        Func<IEnumerable<(int Pid, string CommandLine)>>? processEnumerator = null,
        Func<int, IReadOnlyList<int>>? portResolver = null)
    {
        _processEnumerator = processEnumerator ?? EnumerateWindowsProcesses;
        _portResolver = portResolver ?? ResolveListeningPortsForPid;
    }

    /// <summary>
    /// Discovers an active Antigravity language server endpoint.
    /// </summary>
    /// <returns>Discovered endpoint, or null if no language server is running.</returns>
    public AntigravityEndpoint? DiscoverEndpoint()
    {
        foreach (var (pid, commandLine) in _processEnumerator())
        {

            if (string.IsNullOrEmpty(commandLine))
            {
                continue;
            }

            var match = CsrfTokenRegex.Match(commandLine);

            if (!match.Success)
            {
                continue;
            }

            var csrfToken = match.Groups[1].Value;
            var candidatePorts = _portResolver(pid);

            return new AntigravityEndpoint
            {
                ProcessId = pid,
                CsrfToken = csrfToken,
                CandidatePorts = candidatePorts
            };
        }

        return null;
    }

    private static IEnumerable<(int Pid, string CommandLine)> EnumerateWindowsProcesses()
    {

        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        Process[] processes;

        try
        {
            processes = Process.GetProcessesByName("language_server");
        }
        catch
        {
            yield break;
        }

        foreach (var process in processes)
        {
            using (process)
            {
                string? commandLine = null;

                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            commandLine = obj["CommandLine"]?.ToString();

                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore security or query exceptions
                }

                if (!string.IsNullOrEmpty(commandLine))
                {
                    yield return (process.Id, commandLine);
                }
            }
        }
    }

    private static IReadOnlyList<int> ResolveListeningPortsForPid(int targetPid)
    {

        if (!OperatingSystem.IsWindows() || targetPid <= 0)
        {
            return [];
        }

        var ports = new List<int>();
        int size = 0;
        const uint AF_INET = 2;
        const int TCP_TABLE_OWNER_PID_LISTENER = 3;

        _ = GetExtendedTcpTable(IntPtr.Zero, ref size, true, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0);

        if (size <= 0)
        {
            return ports;
        }

        IntPtr buffer = Marshal.AllocHGlobal(size);

        try
        {
            uint res = GetExtendedTcpTable(buffer, ref size, true, AF_INET, TCP_TABLE_OWNER_PID_LISTENER, 0);

            if (res != 0)
            {
                return ports;
            }

            int numEntries = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                if (row.owningPid == targetPid)
                {
                    int port = ((int)(row.localPort & 0xFF) << 8) | ((int)(row.localPort >> 8) & 0xFF);

                    if (!ports.Contains(port))
                    {
                        ports.Add(port);
                    }
                }

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return ports;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        [MarshalAs(UnmanagedType.Bool)] bool bOrder,
        uint ulAf,
        int tableClass,
        uint reserved);
}
