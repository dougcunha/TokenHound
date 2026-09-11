using System;
using System.ComponentModel;
using System.Diagnostics;

namespace TokenHound.Infrastructure.System;

/// <summary>Provides safe process-name discovery for activity monitors.</summary>
public static class ProcessDiscovery
{
    /// <summary>
    /// Finds the first live process with the specified name and its start time.
    /// </summary>
    /// <param name="processName">The process name without its executable extension.</param>
    /// <returns>The process identifier and UTC start time, or null when unavailable.</returns>
    public static (int Pid, DateTimeOffset StartTimeUtc)? FindProcessByName(string processName)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

        try
        {

            foreach (var process in Process.GetProcessesByName(processName))
            {

                try
                {

                    if (!process.HasExited)
                    {
                        var startTimeUtc = ProcessLiveness.GetProcessStartTimeUtc(process.Id);

                        if (startTimeUtc is not null)
                            return (process.Id, startTimeUtc.Value);
                    }
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
                {

                    // Ignore processes that exit or deny metadata access during enumeration.
                }
                finally
                {

                    process.Dispose();
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {

            // Ignore process enumeration failures.
        }

        return null;
    }
}
