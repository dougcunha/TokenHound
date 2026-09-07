namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Controls diagnostic enrichers attached to the logging pipeline.
/// </summary>
public sealed record LogEnricherSettings
{
    /// <summary>
    /// Gets a value indicating whether the thread ID enricher is enabled.
    /// </summary>
    public bool WithThreadId { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the machine name enricher is enabled.
    /// </summary>
    public bool WithMachineName { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the process ID enricher is enabled.
    /// </summary>
    public bool WithProcessId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the process name enricher is enabled.
    /// </summary>
    public bool WithProcessName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the environment name enricher is enabled.
    /// </summary>
    public bool WithEnvironmentName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the environment user name enricher is enabled.
    /// </summary>
    public bool WithEnvironmentUserName { get; init; }
}
