using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents an HTTP 403 Forbidden response indicating a missing Cline entitlement.
/// </summary>
public sealed class ClineEntitlementException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClineEntitlementException"/> class.
    /// </summary>
    /// <param name="message">The message describing the entitlement failure.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public ClineEntitlementException(
        string message = "Cline account entitlement required (HTTP 403).",
        Exception? innerException = null)
        : base(message, innerException, HttpStatusCode.Forbidden)
    {
    }
}