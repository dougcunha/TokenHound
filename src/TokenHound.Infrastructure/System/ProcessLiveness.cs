using System;
using System.ComponentModel;
using System.Diagnostics;

namespace TokenHound.Infrastructure.System;

/// <summary>
/// Provides process existence and liveness validation with start time comparison
/// to guard against aggressive Windows process identifier (PID) recycling.
/// </summary>
public static class ProcessLiveness
{
    private static readonly TimeSpan DEFAULT_TOLERANCE = TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// Checks whether an operating system process with the specified identifier is alive,
    /// optionally validating that its start time matches the expected timestamp within a tolerance.
    /// </summary>
    /// <param name="pid">The operating system process identifier.</param>
    /// <param name="expectedStartTimeUtc">The optional expected UTC start time of the process.</param>
    /// <param name="tolerance">The optional tolerance threshold for start time comparison.</param>
    /// <returns><see langword="true"/> if the process is alive and matches the expected start time; otherwise, <see langword="false"/>.</returns>
    public static bool IsProcessAlive(
        int pid,
        DateTimeOffset? expectedStartTimeUtc = null,
        TimeSpan? tolerance = null)
    {

        if (pid <= 0)
            return false;

        if (expectedStartTimeUtc is null)
            return CheckProcessExistence(pid);

        var actualStartUtc = GetProcessStartTimeUtc(pid);

        if (actualStartUtc is null)
            return false;

        var effectiveTolerance = tolerance?.Duration() ?? DEFAULT_TOLERANCE;
        var diff = (actualStartUtc.Value.UtcDateTime - expectedStartTimeUtc.Value.UtcDateTime).Duration();

        return diff <= effectiveTolerance;
    }

    /// <summary>
    /// Retrieves the UTC start time of the specified process, or <see langword="null"/> if unavailable.
    /// </summary>
    /// <param name="pid">The operating system process identifier.</param>
    /// <returns>The UTC start time if accessible; otherwise, <see langword="null"/>.</returns>
    public static DateTimeOffset? GetProcessStartTimeUtc(int pid)
    {

        if (pid <= 0)
            return null;

        try
        {

            using var process = Process.GetProcessById(pid);

            if (process.HasExited)
                return null;

            return new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {

            return null;
        }
    }

    private static bool CheckProcessExistence(int pid)
    {

        if (pid <= 0)
            return false;

        try
        {

            using var process = Process.GetProcessById(pid);

            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {

            return false;
        }
    }
}
