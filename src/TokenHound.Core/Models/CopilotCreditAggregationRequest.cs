using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// Supplies verified metadata and parsed dimensions to the Copilot credit policy.
/// </summary>
public sealed record CopilotCreditAggregationRequest
{
    /// <summary>
    /// Gets the verified billing context for the source response.
    /// </summary>
    public required CopilotBillingContext Context { get; init; }

    /// <summary>
    /// Gets the billing period represented by the source response.
    /// </summary>
    public required CopilotBillingPeriod Period { get; init; }

    /// <summary>
    /// Gets the report coverage represented by the source response.
    /// </summary>
    public required CopilotReportCoverage Coverage { get; init; }

    /// <summary>
    /// Gets the source family that produced the dimensions.
    /// </summary>
    public required CopilotCreditSource Source { get; init; }

    /// <summary>
    /// Gets the source reporting timestamp, when supplied.
    /// </summary>
    public DateTimeOffset? SourceAsOfUtc { get; init; }

    /// <summary>
    /// Gets the time when the response was fetched.
    /// </summary>
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source usage is estimated or derived.
    /// </summary>
    public bool IsEstimated { get; init; }

    /// <summary>
    /// Gets the verified product, unit, and optional dimension filters.
    /// </summary>
    public required CopilotCreditFilter Filter { get; init; }

    /// <summary>
    /// Gets the parsed billing dimensions to aggregate.
    /// </summary>
    public required IReadOnlyList<CopilotCreditUsageItem> Items { get; init; }

    /// <summary>
    /// Gets the verified allowance evidence, when the source provided one.
    /// </summary>
    public CopilotAllowanceEvidence? Allowance { get; init; }
}
