namespace TokenHound.Core.Models;

/// <summary>
/// Represents a rate-limit window or quota budget with optional utilization fractions.
/// </summary>
public sealed record LimitWindow
{
    private readonly double? _usedFraction;

    /// <summary>
    /// Gets the identifier or human-readable name of the limit window.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider-defined group that contains the limit window, if reported.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Gets the fraction of the limit used (0.0 to 1.0), or <see langword="null"/> if the total capacity is unknown or null.
    /// </summary>
    public double? UsedFraction
    {
        get =>
            TotalUnits is null ? null : _usedFraction;
        init =>
            _usedFraction = value;
    }

    /// <summary>
    /// Gets the number of remaining units in the current window, if known.
    /// </summary>
    public long? RemainingUnits { get; init; }

    /// <summary>
    /// Gets the exact fractional number of remaining units, if the provider reports one.
    /// </summary>
    public double? RemainingValue { get; init; }

    /// <summary>
    /// Gets the total capacity or maximum units for the window, if known.
    /// </summary>
    public long? TotalUnits { get; init; }

    /// <summary>
    /// Gets the timestamp when this limit window resets, if known.
    /// </summary>
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>
    /// Gets the total duration of the limit window cycle, if known.
    /// </summary>
    public TimeSpan? Period { get; init; }
}
