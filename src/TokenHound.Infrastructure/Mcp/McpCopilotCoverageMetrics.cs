using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains report coverage without source filters.</summary>
public sealed record McpCopilotCoverageMetrics
{
    /// <summary>Gets the first covered report day, if known.</summary>
    [JsonPropertyName("startDay")]
    public DateOnly? StartDay { get; init; }

    /// <summary>Gets the last covered report day, if known.</summary>
    [JsonPropertyName("endDay")]
    public DateOnly? EndDay { get; init; }

    /// <summary>Gets whether the requested report coverage is complete.</summary>
    [JsonPropertyName("isComplete")]
    public required bool IsComplete { get; init; }

    /// <summary>Gets report days expected but unavailable.</summary>
    [JsonPropertyName("missingDays")]
    public required IReadOnlyList<DateOnly> MissingDays { get; init; }

    /// <summary>Gets whether report partitions were missing.</summary>
    [JsonPropertyName("hasMissingPartitions")]
    public required bool HasMissingPartitions { get; init; }

    /// <summary>Gets whether report rows were invalid.</summary>
    [JsonPropertyName("hasInvalidRows")]
    public required bool HasInvalidRows { get; init; }
}
