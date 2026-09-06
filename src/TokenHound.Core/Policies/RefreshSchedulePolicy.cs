using System;

namespace TokenHound.Core.Policies;

/// <summary>
/// Evaluates polling schedules to determine when provider usage data should be refreshed.
/// </summary>
public static class RefreshSchedulePolicy
{
    /// <summary>
    /// Default active polling interval (60 seconds) when at least one agent is busy.
    /// </summary>
    public static readonly TimeSpan DEFAULT_ACTIVE_INTERVAL = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Default idle polling interval (300 seconds / 5 minutes) when all agents are idle.
    /// </summary>
    public static readonly TimeSpan DEFAULT_IDLE_INTERVAL = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Default stale grace margin (900 seconds / 15 minutes) after which an unrefreshed reading is considered stale.
    /// </summary>
    public static readonly TimeSpan DEFAULT_STALE_THRESHOLD = TimeSpan.FromSeconds(900);

    /// <summary>
    /// Gets the default active polling interval (60 seconds).
    /// </summary>
    public static TimeSpan DefaultActiveInterval
        => DEFAULT_ACTIVE_INTERVAL;

    /// <summary>
    /// Gets the default idle polling interval (300 seconds).
    /// </summary>
    public static TimeSpan DefaultIdleInterval
        => DEFAULT_IDLE_INTERVAL;

    /// <summary>
    /// Gets the default stale threshold (900 seconds).
    /// </summary>
    public static TimeSpan DefaultStaleThreshold
        => DEFAULT_STALE_THRESHOLD;

    /// <summary>
    /// Determines whether a provider refresh should be dispatched.
    /// Rule: executes refresh if <paramref name="isBusy"/> is <see langword="true"/> OR <paramref name="timeSinceLastAttempt"/> &gt;= <paramref name="idleInterval"/>.
    /// </summary>
    /// <param name="isBusy"><see langword="true"/> if any agent session is currently active or busy; otherwise, <see langword="false"/>.</param>
    /// <param name="timeSinceLastAttempt">The elapsed duration since the last refresh attempt.</param>
    /// <param name="idleInterval">The configured idle polling interval threshold.</param>
    /// <returns><see langword="true"/> if a refresh should occur; otherwise, <see langword="false"/>.</returns>
    public static bool ShouldRefresh(
        bool isBusy,
        TimeSpan timeSinceLastAttempt,
        TimeSpan idleInterval)
    {
        return isBusy || timeSinceLastAttempt >= idleInterval;
    }

    /// <summary>
    /// Determines whether a provider refresh should be dispatched using the default idle interval (300 seconds).
    /// </summary>
    /// <param name="isBusy"><see langword="true"/> if any agent session is currently active or busy; otherwise, <see langword="false"/>.</param>
    /// <param name="timeSinceLastAttempt">The elapsed duration since the last refresh attempt.</param>
    /// <returns><see langword="true"/> if a refresh should occur; otherwise, <see langword="false"/>.</returns>
    public static bool ShouldRefresh(bool isBusy, TimeSpan timeSinceLastAttempt)
        => ShouldRefresh(isBusy, timeSinceLastAttempt, DEFAULT_IDLE_INTERVAL);
}
