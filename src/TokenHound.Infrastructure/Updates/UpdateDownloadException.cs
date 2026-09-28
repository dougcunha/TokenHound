using System;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Thrown when an update download is refused or fails verification; nothing was kept on disk.
/// </summary>
public sealed class UpdateDownloadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDownloadException"/> class.
    /// </summary>
    public UpdateDownloadException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDownloadException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UpdateDownloadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDownloadException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying failure.</param>
    public UpdateDownloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDownloadException"/> class for a verification failure.
    /// </summary>
    /// <param name="failure">The failure category.</param>
    /// <param name="message">The error message.</param>
    public UpdateDownloadException(UpdateDownloadFailure failure, string message)
        : base(message)
    {

        Failure = failure;
    }

    /// <summary>
    /// Gets the failure category, when the download was refused or failed verification.
    /// </summary>
    public UpdateDownloadFailure? Failure { get; }
}
