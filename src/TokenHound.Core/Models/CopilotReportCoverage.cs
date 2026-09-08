using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// Describes the dates, filters, and completeness of a Copilot usage report.
/// </summary>
public sealed record CopilotReportCoverage
{
    /// <summary>
    /// Gets the first covered report day, when known.
    /// </summary>
    public DateOnly? StartDay { get; init; }

    /// <summary>
    /// Gets the last covered report day, when known.
    /// </summary>
    public DateOnly? EndDay { get; init; }

    /// <summary>
    /// Gets a value indicating whether the requested report coverage is complete.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Gets the report days that were expected but unavailable.
    /// </summary>
    public IReadOnlyList<DateOnly> MissingDays { get; init; } = [];

    /// <summary>
    /// Gets the normalized source filters applied to the report.
    /// </summary>
    public IReadOnlyDictionary<string, string> Filters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether one or more report partitions were missing.
    /// </summary>
    public bool HasMissingPartitions { get; init; }

    /// <summary>
    /// Gets a value indicating whether one or more report rows were invalid.
    /// </summary>
    public bool HasInvalidRows { get; init; }
}
