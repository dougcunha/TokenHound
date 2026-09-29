using System;

namespace TokenHound.Core.Policies;

/// <summary>
/// Decodes the Windows "Startup apps" approval value that Explorer keeps for each Startup-folder entry.
/// </summary>
public static class StartupApprovalPolicy
{
    /// <summary>
    /// Determines whether an approval value marks its startup entry as disabled.
    /// </summary>
    /// <param name="value">The raw approval value, or an empty span when Windows stores none.</param>
    /// <returns><see langword="true"/> when the first byte is odd (disabled); <see langword="false"/> for an empty or even-first-byte value.</returns>
    public static bool IsDisabled(ReadOnlySpan<byte> value)
        => !value.IsEmpty && (value[0] & 1) == 1;
}
