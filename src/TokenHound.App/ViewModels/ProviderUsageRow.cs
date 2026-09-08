using System;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents an immutable, WPF-free view model for an individual provider usage or quota row.
/// </summary>
public sealed record ProviderUsageRow
{
    /// <summary>Gets the unique identifier of the row.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the human-readable display label of the quota or credit window.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the utilization fraction (0.0 to 1.0), or null if unmeasured.</summary>
    public double? UsedFraction { get; init; }

    /// <summary>Gets the utilization fraction clamped between 0.0 and 1.0 for progress display.</summary>
    public double ClampedFraction
        => UsedFraction.HasValue ? Math.Clamp(UsedFraction.Value, 0.0, 1.0) : 0.0;

    /// <summary>Gets a value indicating whether a progress bar should be displayed.</summary>
    public bool HasProgress
        => UsedFraction.HasValue;

    /// <summary>Gets the primary quantity or balance display text.</summary>
    public string? PrimaryQuantityText { get; init; }

    /// <summary>Gets the secondary breakdown or total quantity text, or null if unneeded.</summary>
    public string? SecondaryQuantityText { get; init; }

    /// <summary>Gets a value indicating whether secondary quantity text is present.</summary>
    public bool HasSecondaryQuantity
        => !string.IsNullOrWhiteSpace(SecondaryQuantityText);

    /// <summary>Gets the account or organizational scope text, or null if unevidenced.</summary>
    public string? ScopeText { get; init; }

    /// <summary>Gets a value indicating whether scope text is present.</summary>
    public bool HasScope
        => !string.IsNullOrWhiteSpace(ScopeText);

    /// <summary>Gets the formatted countdown or reset timing text, or null if unmeasured.</summary>
    public string? ResetText { get; init; }

    /// <summary>Gets a value indicating whether reset text is present.</summary>
    public bool HasReset
        => !string.IsNullOrWhiteSpace(ResetText);

    /// <summary>Gets the source, coverage, and freshness provenance text.</summary>
    public string? ProvenanceText { get; init; }

    /// <summary>Gets a value indicating whether provenance text is present.</summary>
    public bool HasProvenance
        => !string.IsNullOrWhiteSpace(ProvenanceText);

    /// <summary>Gets the actionable error or unavailable guidance text, or null if normal.</summary>
    public string? ErrorText { get; init; }

    /// <summary>Gets a value indicating whether error text is present.</summary>
    public bool HasError
        => !string.IsNullOrWhiteSpace(ErrorText);
}
