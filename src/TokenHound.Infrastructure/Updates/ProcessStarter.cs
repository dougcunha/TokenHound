using System.Diagnostics;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Default process start seam for the update appliers.
/// </summary>
internal static class ProcessStarter
{
    /// <summary>
    /// Starts a process without waiting for it.
    /// </summary>
    /// <param name="startInfo">The start information.</param>
    /// <returns><see langword="true"/> when a process was started.</returns>
    internal static bool Start(ProcessStartInfo startInfo)
    {

        using var process = Process.Start(startInfo);

        return process is not null;
    }
}
