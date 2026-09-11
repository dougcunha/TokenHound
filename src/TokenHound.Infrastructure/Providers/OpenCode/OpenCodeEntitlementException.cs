using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Represents an HTTP 403 Forbidden response indicating missing OpenCode Go subscription entitlement.
/// </summary>
public sealed class OpenCodeEntitlementException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeEntitlementException"/> class.
    /// </summary>
    /// <param name="message">The message describing the subscription entitlement failure.</param>
    /// <param name="errorResponse">The deserialized error response payload, if present.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public OpenCodeEntitlementException(
        string message = "OpenCode Go subscription entitlement required (HTTP 403).",
        OpenCodeErrorResponse? errorResponse = null,
        Exception? innerException = null)
        : base(message, innerException, HttpStatusCode.Forbidden)
    {

        ErrorResponse = errorResponse;
    }

    /// <summary>
    /// Gets the structured error response payload, if deserialized.
    /// </summary>
    public OpenCodeErrorResponse? ErrorResponse { get; }
}
