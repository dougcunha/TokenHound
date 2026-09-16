using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents an HTTP 429 Too Many Requests response from the Cline account API.
/// </summary>
public sealed class ClineRateLimitException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClineRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The message describing the rate-limit failure.</param>
    /// <param name="retryAfterSeconds">The retry-after penalty in seconds, if reported by the server.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public ClineRateLimitException(
        string message,
        int? retryAfterSeconds = null,
        Exception? innerException = null)
        : base(message, innerException, HttpStatusCode.TooManyRequests)
    {

        RetryAfterSeconds = retryAfterSeconds;
    }

    /// <summary>
    /// Gets the server-reported Retry-After delay in seconds, if specified.
    /// </summary>
    public int? RetryAfterSeconds { get; }
}