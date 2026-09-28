using System;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Thrown when a downloaded update cannot be applied; the current installation was left or restored in place.
/// </summary>
public sealed class UpdateApplyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateApplyException"/> class.
    /// </summary>
    public UpdateApplyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateApplyException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UpdateApplyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateApplyException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying failure.</param>
    public UpdateApplyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
