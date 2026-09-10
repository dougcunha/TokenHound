using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers;

/// <summary>
/// Preserves a provider HTTP failure and its optional server retry penalty without retaining response content.
/// </summary>
public sealed class ProviderHttpException : HttpRequestException
{
    /// <summary>
    /// Initializes a provider HTTP failure.
    /// </summary>
    /// <param name="message">A non-sensitive diagnostic.</param>
    /// <param name="statusCode">The HTTP response status.</param>
    /// <param name="retryAfterSeconds">The parsed Retry-After value, if present.</param>
    public ProviderHttpException(
        string message,
        HttpStatusCode statusCode,
        int? retryAfterSeconds = null)
        : base(message, null, statusCode)
    {

        RetryAfterSeconds = retryAfterSeconds;
    }

    /// <summary>Gets the parsed Retry-After value in seconds, if present.</summary>
    public int? RetryAfterSeconds { get; }
}
