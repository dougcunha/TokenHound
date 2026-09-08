using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Configuration for HTTP 429 rate-limit resilience parameters.
/// </summary>
public sealed record RateLimitSettings
{
    /// <summary>
    /// The absolute lowest retry floor permitted (60 seconds).
    /// </summary>
    public const int MINIMUM_FLOOR_SECONDS = 60;

    /// <summary>
    /// The default retry floor applied when no configuration is specified (60 seconds).
    /// </summary>
    public const int DEFAULT_FLOOR_SECONDS = 60;

    /// <summary>
    /// Gets the configured retry floor in seconds, or <see langword="null"/> to use the default floor.
    /// </summary>
    public int? MinimumRetryFloorSeconds { get; init; }

    /// <summary>
    /// Gets the effective retry floor duration, clamped to at least <see cref="MINIMUM_FLOOR_SECONDS"/>.
    /// </summary>
    [JsonIgnore]
    public TimeSpan MinimumRetryFloor
        => TimeSpan.FromSeconds(Math.Max(MINIMUM_FLOOR_SECONDS, MinimumRetryFloorSeconds ?? DEFAULT_FLOOR_SECONDS));
}
