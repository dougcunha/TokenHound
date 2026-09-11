using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Represents an HTTP 401 Unauthorized response from the OpenCode Go API.
/// </summary>
public sealed class OpenCodeAuthException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeAuthException"/> class.
    /// </summary>
    /// <param name="message">The message describing the authentication failure.</param>
    /// <param name="errorResponse">The deserialized error response payload, if present.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public OpenCodeAuthException(
        string message = "OpenCode API request unauthorized (HTTP 401).",
        OpenCodeErrorResponse? errorResponse = null,
        Exception? innerException = null)
        : base(message, innerException, HttpStatusCode.Unauthorized)
    {

        ErrorResponse = errorResponse;
    }

    /// <summary>
    /// Gets the structured error response payload, if deserialized.
    /// </summary>
    public OpenCodeErrorResponse? ErrorResponse { get; }
}
