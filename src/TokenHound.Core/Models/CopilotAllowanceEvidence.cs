using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// Records the authoritative evidence for an included Copilot credit allowance.
/// </summary>
public sealed record CopilotAllowanceEvidence
{
    /// <summary>
    /// Gets the JSON path that supplied the allowance value.
    /// </summary>
    public required string JsonPath { get; init; }

    /// <summary>
    /// Gets the official documentation URL describing the allowance field.
    /// </summary>
    public required string DocumentationUrl { get; init; }

    /// <summary>
    /// Gets the evidence identifier for the verified mapping.
    /// </summary>
    public required string EvidenceKey { get; init; }

    /// <summary>
    /// Gets the usage unit associated with the allowance.
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Gets the billing context to which the allowance applies.
    /// </summary>
    public required CopilotBillingContext Context { get; init; }

    /// <summary>
    /// Gets the billing period to which the allowance applies.
    /// </summary>
    public required CopilotBillingPeriod Period { get; init; }

    /// <summary>
    /// Gets the authoritative included credit quantity.
    /// </summary>
    public required decimal Value { get; init; }

    /// <summary>
    /// Gets the filters covered by the allowance, when the source narrows them.
    /// </summary>
    public IReadOnlyDictionary<string, string> Filters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
