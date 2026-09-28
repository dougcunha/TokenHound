using System;

namespace TokenHound.App;

/// <summary>
/// Constants of the update relaunch handshake.
/// </summary>
internal static class UpdateStartup
{
    /// <summary>How long a relaunched process waits for the previous instance to release the instance mutex.</summary>
    public static readonly TimeSpan PREVIOUS_INSTANCE_WAIT = TimeSpan.FromSeconds(30);

    /// <summary>The executable file name used when the process path is unavailable.</summary>
    public const string DEFAULT_EXECUTABLE_NAME = "TokenHound.App.exe";
}
