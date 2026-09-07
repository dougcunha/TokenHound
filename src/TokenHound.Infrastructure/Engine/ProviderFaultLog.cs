using Serilog;
using System;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Records provider refresh faults that are absorbed into a stale snapshot instead of propagating to the caller.
/// </summary>
internal static class ProviderFaultLog
{
    /// <summary>
    /// Records a refresh aborted by the provider itself rather than by the store's cancellation token.
    /// </summary>
    /// <param name="providerId">The provider whose refresh was aborted.</param>
    /// <param name="ex">The cancellation exception raised by the provider.</param>
    public static void Aborted(string providerId, Exception ex)
    {

        Log.Warning(
            ex,
            "Provider {ProviderId} refresh was aborted before completing; keeping the previous reading as stale",
            providerId
        );
    }

    /// <summary>
    /// Records a refresh that failed with an unhandled fault.
    /// </summary>
    /// <param name="providerId">The provider whose refresh failed.</param>
    /// <param name="ex">The fault raised while fetching the snapshot.</param>
    public static void Failed(string providerId, Exception ex)
    {

        Log.Error(
            ex,
            "Provider {ProviderId} refresh failed; keeping the previous reading as stale",
            providerId
        );
    }
}
