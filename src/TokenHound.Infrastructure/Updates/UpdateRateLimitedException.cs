using System;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Thrown when GitHub answers a release query with 429, or with 403 and an exhausted rate-limit quota.
/// </summary>
public sealed class UpdateRateLimitedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRateLimitedException"/> class.
    /// </summary>
    public UpdateRateLimitedException()
        : this((int?)null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRateLimitedException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UpdateRateLimitedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRateLimitedException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying failure.</param>
    public UpdateRateLimitedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRateLimitedException"/> class with the server hints.
    /// </summary>
    /// <param name="retryAfterSeconds">The <c>Retry-After</c> seconds, if present.</param>
    /// <param name="resetUtc">The <c>X-RateLimit-Reset</c> instant, if present.</param>
    public UpdateRateLimitedException(int? retryAfterSeconds, DateTimeOffset? resetUtc)
        : base("GitHub rate-limited the update check.")
    {

        RetryAfterSeconds = retryAfterSeconds;
        ResetUtc = resetUtc;
    }

    /// <summary>
    /// Gets the <c>Retry-After</c> seconds reported by GitHub, if any.
    /// </summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>
    /// Gets the <c>X-RateLimit-Reset</c> instant reported by GitHub, if any.
    /// </summary>
    public DateTimeOffset? ResetUtc { get; }
}
