using System;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents one quota or credit column of an account row in the provider status window.
/// </summary>
public sealed record ProviderStatusColumn
{
    /// <summary>Gets the key of the usage row this column projects.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the quota or credit window label, identical to the HUD row label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the used percentage text, or the quantity text when no used fraction is known.</summary>
    public required string ValueText { get; init; }

    /// <summary>Gets the used fraction (0.0 to 1.0), or null when unmeasured.</summary>
    public double? UsedFraction { get; init; }

    /// <summary>Gets the used fraction clamped between 0.0 and 1.0 for the progress bar.</summary>
    public double BarFraction
        => UsedFraction.HasValue ? Math.Clamp(UsedFraction.Value, 0.0, 1.0) : 0.0;

    /// <summary>Gets a value indicating whether a progress bar is drawn.</summary>
    public bool HasBar
        => UsedFraction.HasValue;

    /// <summary>Gets the colour level of the progress bar.</summary>
    public required UsageLevel Level { get; init; }

    /// <summary>Gets the reset line, relative and absolute, or the no-reset text.</summary>
    public required string ResetText { get; init; }

    /// <summary>Gets the reset instant of the window, or null when no reset is pending.</summary>
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>Gets a value indicating whether the window is exhausted (100% used or more).</summary>
    public bool IsExhausted { get; init; }
}
