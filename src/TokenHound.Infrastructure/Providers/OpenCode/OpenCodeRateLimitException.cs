using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Represents an HTTP 429 Too Many Requests response from the OpenCode Go API.
/// </summary>
public sealed class OpenCodeRateLimitException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The message describing the rate-limit failure.</param>
    /// <param name="retryAfterSeconds">The retry-after penalty in seconds, if reported by the server.</param>
    /// <param name="errorResponse">The deserialized error response payload, if present.</param>
    /// <param name="innerException">The optional inner exception.</param>
    /// <param name="resetTimeUtc">The server-reported quota reset timestamp, if present.</param>
    public OpenCodeRateLimitException(
        string message,
        int? retryAfterSeconds = null,
        OpenCodeErrorResponse? errorResponse = null,
        Exception? innerException = null,
        DateTimeOffset? resetTimeUtc = null)
        : base(message, innerException, HttpStatusCode.TooManyRequests)
    {

        RetryAfterSeconds = retryAfterSeconds;
        ErrorResponse = errorResponse;
        ResetTimeUtc = resetTimeUtc;
    }

    /// <summary>
    /// Gets the server-reported Retry-After delay in seconds, if specified.
    /// </summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>
    /// Gets the structured error response payload, if deserialized.
    /// </summary>
    public OpenCodeErrorResponse? ErrorResponse { get; }

    /// <summary>
    /// Gets the server-reported quota reset timestamp, if specified.
    /// </summary>
    public DateTimeOffset? ResetTimeUtc { get; }
}
