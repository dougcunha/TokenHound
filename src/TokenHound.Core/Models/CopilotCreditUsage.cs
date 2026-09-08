using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// Represents compatible Copilot credit quantities and their source qualifications.
/// </summary>
public sealed record CopilotCreditUsage
{
    /// <summary>
    /// Gets the verified billing context for the quantities.
    /// </summary>
    public required CopilotBillingContext Context { get; init; }

    /// <summary>
    /// Gets the verified billing period for the quantities.
    /// </summary>
    public required CopilotBillingPeriod Period { get; init; }

    /// <summary>
    /// Gets the report coverage for the quantities.
    /// </summary>
    public required CopilotReportCoverage Coverage { get; init; }

    /// <summary>
    /// Gets the normalized unit used by the quantities.
    /// </summary>
    public string? UnitType { get; init; }

    /// <summary>
    /// Gets the aggregated gross quantity.
    /// </summary>
    public decimal? GrossUsed { get; init; }

    /// <summary>
    /// Gets the aggregated discounted or included quantity.
    /// </summary>
    public decimal? DiscountedUsed { get; init; }

    /// <summary>
    /// Gets the aggregated net or additional quantity.
    /// </summary>
    public decimal? NetUsed { get; init; }

    /// <summary>
    /// Gets the compatible included allowance, when verified.
    /// </summary>
    public decimal? IncludedTotal { get; init; }

    /// <summary>
    /// Gets the signed remaining quantity, when the allowance and usage are compatible.
    /// </summary>
    public decimal? Remaining { get; init; }

    /// <summary>
    /// Gets the unbounded used fraction, or null when no positive total is available.
    /// </summary>
    public decimal? UsedFraction { get; init; }

    /// <summary>
    /// Gets the evidence used for the included allowance, when present.
    /// </summary>
    public CopilotAllowanceEvidence? Allowance { get; init; }

    /// <summary>
    /// Gets the source family that produced the quantities.
    /// </summary>
    public required CopilotCreditSource Source { get; init; }

    /// <summary>
    /// Gets the source reporting timestamp, when supplied.
    /// </summary>
    public DateTimeOffset? SourceAsOfUtc { get; init; }

    /// <summary>
    /// Gets the time when the source response was fetched.
    /// </summary>
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the quantities are estimated or derived.
    /// </summary>
    public bool IsEstimated { get; init; }

    /// <summary>
    /// Gets the normalized filters represented by the coverage.
    /// </summary>
    public IReadOnlyDictionary<string, string> Filters
        => Coverage.Filters;
}
