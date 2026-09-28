using System;
using System.IO;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Detects how the running copy was deployed and whether its folder accepts an in-place update.
/// </summary>
public static class InstallModeDetector
{
    /// <summary>
    /// The marker file the installer places next to the executable; the portable zip never contains it.
    /// </summary>
    public const string MARKER_FILE_NAME = "TokenHound.installed";

    /// <summary>
    /// The temporary file used to probe whether the application folder is writable.
    /// </summary>
    public const string PROBE_FILE_NAME = "~update-probe.tmp";

    /// <summary>
    /// Detects the install mode from the installer marker file.
    /// </summary>
    /// <param name="appDirectory">The directory containing the running executable.</param>
    /// <returns><see cref="InstallMode.Installed"/> when the marker exists; otherwise <see cref="InstallMode.Portable"/>.</returns>
    public static InstallMode DetectMode(string appDirectory)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(appDirectory);

        return File.Exists(Path.Combine(appDirectory, MARKER_FILE_NAME)) ? InstallMode.Installed : InstallMode.Portable;
    }

    /// <summary>
    /// Probes whether files can be created in <paramref name="appDirectory"/> by creating and deleting a temporary file.
    /// </summary>
    /// <param name="appDirectory">The directory containing the running executable.</param>
    /// <returns><see langword="true"/> when the probe file could be created; otherwise <see langword="false"/>.</returns>
    public static bool IsWritable(string appDirectory)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(appDirectory);

        try
        {

            using var probe = new FileStream(
                Path.Combine(appDirectory, PROBE_FILE_NAME),
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1,
                FileOptions.DeleteOnClose
            );

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {

            return false;
        }
    }
}
