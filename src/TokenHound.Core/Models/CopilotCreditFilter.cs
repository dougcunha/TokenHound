namespace TokenHound.Core.Models;

/// <summary>
/// Defines the verified billing dimensions eligible for aggregation.
/// </summary>
public sealed record CopilotCreditFilter
{
    /// <summary>
    /// Gets the verified product identifier.
    /// </summary>
    public required string Product { get; init; }

    /// <summary>
    /// Gets the verified unit identifier.
    /// </summary>
    public required string UnitType { get; init; }

    /// <summary>
    /// Gets the optional verified SKU identifier.
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Gets the optional verified model identifier.
    /// </summary>
    public string? Model { get; init; }
}
