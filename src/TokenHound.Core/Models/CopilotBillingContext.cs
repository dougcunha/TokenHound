namespace TokenHound.Core.Models;

/// <summary>
/// Identifies the verified principal and billing owner for Copilot usage.
/// </summary>
public sealed record CopilotBillingContext
{
    /// <summary>
    /// Gets the verified principal identifier associated with the reading.
    /// </summary>
    public required string PrincipalId { get; init; }

    /// <summary>
    /// Gets the billing owner scope.
    /// </summary>
    public required CopilotBillingScope Scope { get; init; }

    /// <summary>
    /// Gets the stable billing owner identifier, when known.
    /// </summary>
    public string? OwnerId { get; init; }

    /// <summary>
    /// Gets the display name of the billing owner, when known.
    /// </summary>
    public string? OwnerName { get; init; }

    /// <summary>
    /// Gets the normalized plan classification.
    /// </summary>
    public required CopilotPlanType Plan { get; init; }

    /// <summary>
    /// Gets the provider plan name before normalization, when supplied.
    /// </summary>
    public string? RawPlan { get; init; }

    /// <summary>
    /// Gets the evidence identifier used to establish the billing context, when known.
    /// </summary>
    public string? EvidenceKey { get; init; }
}
