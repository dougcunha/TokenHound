namespace TokenHound.Infrastructure.Providers.Mock;

/// <summary>
/// Defines predefined operational scenarios for <see cref="MockUsageProvider"/>.
/// </summary>
public enum MockScenario
{
    /// <summary>
    /// Normal operational state with low session (20%) and weekly (15%) utilization.
    /// </summary>
    Normal,

    /// <summary>
    /// Warning operational state with high weekly (85%) utilization.
    /// </summary>
    Warning,

    /// <summary>
    /// Rate-limited operational state with an active block and retry countdown.
    /// </summary>
    RateLimited,

    /// <summary>
    /// Authentication required operational state.
    /// </summary>
    NeedsAuth,

    /// <summary>
    /// Stale data operational state captured in the past.
    /// </summary>
    Stale,

    /// <summary>
    /// Unidirectional quota operational state with no total capacity or used fraction.
    /// </summary>
    Unidirectional
}
