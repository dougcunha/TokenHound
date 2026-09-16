using System;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents an HTTP 401 Unauthorized response from the Cline account API.
/// </summary>
public sealed class ClineAuthException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClineAuthException"/> class.
    /// </summary>
    /// <param name="message">The message describing the authentication failure.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public ClineAuthException(
        string message = "Cline account request unauthorized (HTTP 401).",
        Exception? innerException = null)
        : base(message, innerException, HttpStatusCode.Unauthorized)
    {
    }
}