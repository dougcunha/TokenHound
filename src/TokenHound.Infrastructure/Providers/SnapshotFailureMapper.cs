using System;
using System.Net;

namespace TokenHound.Infrastructure.Providers;

/// <summary>
/// Classifies provider HTTP failures into a shared outcome before a provider builds its snapshot.
/// </summary>
internal static class SnapshotFailureMapper
{
    /// <summary>
    /// Identifies the shared outcome of a provider failure.
    /// </summary>
    internal enum Outcome
    {
        /// <summary>The provider must be re-authenticated.</summary>
        NeedsAuth,

        /// <summary>The credential lacks the required entitlement.</summary>
        AccessDenied,

        /// <summary>The provider is temporarily rate limited.</summary>
        RateLimited,

        /// <summary>The failure could not be classified further.</summary>
        Stale,

        /// <summary>The provider has no usable quota or entitlement.</summary>
        Unsupported,

        /// <summary>The failure is handled by a fallback and yields no snapshot.</summary>
        Ignored
    }

    /// <summary>
    /// Classifies the HTTP status using the shared decision core, allowing a provider hook to
    /// override individual statuses.
    /// </summary>
    /// <param name="statusCode">The reported HTTP status, if any.</param>
    /// <param name="hasCredential">Whether a usable credential accompanied the request.</param>
    /// <param name="overrideHook">An optional hook returning a status-specific outcome.</param>
    /// <returns>The classified shared outcome.</returns>
    internal static Outcome Classify(
        HttpStatusCode? statusCode,
        bool hasCredential,
        Func<HttpStatusCode?, Outcome?>? overrideHook = null)
    {

        if (overrideHook?.Invoke(statusCode) is { } overridden)
            return overridden;

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => Outcome.NeedsAuth,
            HttpStatusCode.Forbidden => hasCredential ? Outcome.AccessDenied : Outcome.NeedsAuth,
            HttpStatusCode.TooManyRequests => Outcome.RateLimited,
            _ => Outcome.Stale
        };
    }
}
