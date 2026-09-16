using System;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents a timeout condition while communicating with the Cline account API.
/// </summary>
public sealed class ClineTimeoutException : TimeoutException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClineTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The message describing the timeout.</param>
    /// <param name="innerException">The optional inner exception.</param>
    public ClineTimeoutException(
        string message = "Cline account request timed out.",
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}