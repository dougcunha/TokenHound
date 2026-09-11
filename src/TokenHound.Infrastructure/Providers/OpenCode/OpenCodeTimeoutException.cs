using System;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Represents a timeout condition while communicating with the OpenCode Go API.
/// </summary>
public sealed class OpenCodeTimeoutException : TimeoutException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The message describing the timeout.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public OpenCodeTimeoutException(
        string message = "OpenCode API request timed out.",
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
