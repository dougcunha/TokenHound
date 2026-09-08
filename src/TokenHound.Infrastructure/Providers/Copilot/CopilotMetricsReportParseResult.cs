using System;
using System.Collections.Generic;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Contains the aggregated results and validation status of parsing one or more report partitions.
/// </summary>
public sealed record CopilotMetricsReportParseResult
{
    /// <summary>
    /// Gets the reporting day.
    /// </summary>
    public required DateOnly Day { get; init; }

    /// <summary>
    /// Gets the total AI credits consumed across all valid deduplicated users.
    /// </summary>
    public decimal TotalCreditsUsed { get; init; }

    /// <summary>
    /// Gets the number of distinct valid users contributing to this report.
    /// </summary>
    public int UserCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether any invalid or malformed rows were encountered.
    /// </summary>
    public bool HasInvalidRows { get; init; }

    /// <summary>
    /// Gets a value indicating whether the day was invalidated due to conflicting duplicates.
    /// </summary>
    public bool IsDayInvalid { get; init; }

    /// <summary>
    /// Gets the parsed row for the authenticated user principal, if found.
    /// </summary>
    public CopilotMetricsUserRow? PrincipalRow { get; init; }

    /// <summary>
    /// Gets the deduplicated valid user rows.
    /// </summary>
    public IReadOnlyList<CopilotMetricsUserRow> UserRows { get; init; } = [];
}
