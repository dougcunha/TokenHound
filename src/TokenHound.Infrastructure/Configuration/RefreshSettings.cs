using System;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted polling cadence of the usage store, expressed in seconds.
/// </summary>
public sealed record RefreshSettings
{
    /// <summary>
    /// The smallest accepted interval (30 seconds), guarding against configurations that hammer provider APIs.
    /// </summary>
    public const int MINIMUM_INTERVAL_SECONDS = 30;

    /// <summary>
    /// Gets the timer tick interval used while at least one agent is busy, or <see langword="null"/> to use the default.
    /// </summary>
    public int? ActiveIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the interval that must elapse before an idle refresh is dispatched, or <see langword="null"/> to use the default.
    /// </summary>
    public int? IdleIntervalSeconds { get; init; }

    /// <summary>
    /// Gets the resolved active polling interval, falling back to <see cref="RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL"/>.
    /// </summary>
    public TimeSpan ActiveInterval
        => Resolve(ActiveIntervalSeconds, RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL);

    /// <summary>
    /// Gets the resolved idle polling interval, falling back to <see cref="RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL"/>.
    /// </summary>
    public TimeSpan IdleInterval
        => Resolve(IdleIntervalSeconds, RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL);

    private static TimeSpan Resolve(int? configuredSeconds, TimeSpan fallback)
    {

        if (configuredSeconds is null || configuredSeconds.Value < MINIMUM_INTERVAL_SECONDS)
            return fallback;

        return TimeSpan.FromSeconds(configuredSeconds.Value);
    }
}
