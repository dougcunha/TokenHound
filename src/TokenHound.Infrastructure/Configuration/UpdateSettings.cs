using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted automatic update preferences: whether periodic checks run, their interval, and the skipped version.
/// </summary>
public sealed record UpdateSettings
{
    /// <summary>
    /// The default periodic check interval (24 hours).
    /// </summary>
    public const int DEFAULT_INTERVAL_HOURS = 24;

    /// <summary>
    /// The largest accepted periodic check interval (720 hours, 30 days).
    /// </summary>
    public const int MAXIMUM_INTERVAL_HOURS = 720;

    /// <summary>
    /// Gets whether periodic update checks are enabled, or <see langword="null"/> to use the default (enabled).
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the periodic check interval in hours, where 0 turns periodic checks off, or <see langword="null"/> for the default.
    /// </summary>
    public int? CheckIntervalHours { get; init; }

    /// <summary>
    /// Gets the version the user chose to skip, without the <c>v</c> prefix, or <see langword="null"/> when none.
    /// </summary>
    public string? SkippedVersion { get; init; }

    /// <summary>
    /// Gets a value indicating whether periodic update checks are enabled.
    /// </summary>
    [JsonIgnore]
    public bool IsEnabled
        => Enabled ?? true;

    /// <summary>
    /// Gets the effective interval in hours, clamped to 0..<see cref="MAXIMUM_INTERVAL_HOURS"/>.
    /// </summary>
    [JsonIgnore]
    public int IntervalHours
        => Math.Clamp(CheckIntervalHours ?? DEFAULT_INTERVAL_HOURS, 0, MAXIMUM_INTERVAL_HOURS);
}
