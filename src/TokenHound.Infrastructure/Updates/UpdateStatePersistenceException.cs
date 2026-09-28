using System;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Thrown when a rate-limit deadline for update checks cannot be persisted; update checks are then disabled for the process.
/// </summary>
public sealed class UpdateStatePersistenceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatePersistenceException"/> class.
    /// </summary>
    public UpdateStatePersistenceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatePersistenceException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UpdateStatePersistenceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatePersistenceException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The persistence failure.</param>
    public UpdateStatePersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
