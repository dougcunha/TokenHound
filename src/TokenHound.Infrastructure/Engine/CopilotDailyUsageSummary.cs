using System;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Represents a stored completed day summary for Copilot historical reports.
/// </summary>
public sealed record CopilotDailyUsageSummary
{
    /// <summary>
    /// Gets the date of the reported metrics.
    /// </summary>
    public required DateOnly Day { get; init; }

    /// <summary>
    /// Gets the total AI credits used on this day.
    /// </summary>
    public required decimal AiCreditsUsed { get; init; }

    /// <summary>
    /// Gets the number of users contributing to this day.
    /// </summary>
    public int UserCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether report partitions were missing.
    /// </summary>
    public bool HasMissingPartitions { get; init; }

    /// <summary>
    /// Gets a value indicating whether invalid rows were encountered.
    /// </summary>
    public bool HasInvalidRows { get; init; }

    /// <summary>
    /// Gets the timestamp when this summary was fetched.
    /// </summary>
    public required DateTimeOffset FetchedAtUtc { get; init; }
}
