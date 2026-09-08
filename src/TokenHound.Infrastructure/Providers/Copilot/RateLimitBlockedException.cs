using System;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Exception thrown when a Copilot request is blocked by the rate-limit gate.
/// </summary>
public sealed class RateLimitBlockedException : InvalidOperationException
{
    /// <summary>Gets the UTC deadline when requests may resume.</summary>
    public DateTimeOffset DeadlineUtc { get; }

    /// <summary>Gets the server-reported Retry-After seconds, if any.</summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>Gets a value indicating whether this block was caused by a persistence failure.</summary>
    public bool IsPersistenceFailure { get; }

    /// <summary>Initializes a new rate-limit blocked exception.</summary>
    public RateLimitBlockedException(
        DateTimeOffset deadlineUtc,
        int? retryAfterSeconds = null,
        bool isPersistenceFailure = false,
        string? message = null,
        Exception? innerException = null)
        : base(
            message ?? $"Copilot HTTP request blocked: rate-limit deadline active until {deadlineUtc:O}.",
            innerException
        )
    {

        DeadlineUtc = deadlineUtc;
        RetryAfterSeconds = retryAfterSeconds;
        IsPersistenceFailure = isPersistenceFailure;
    }
}
