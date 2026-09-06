using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Monitors Antigravity process and reasoning activity using a 45-second write threshold.
/// </summary>
public sealed class AntigravityActivityMonitor : IActivityMonitor
{
    private const string PROVIDER_ID = "gemini";
    private static readonly TimeSpan DefaultActivityThreshold = TimeSpan.FromSeconds(45);

    private readonly AntigravityEndpointDiscovery _discovery;
    private readonly AntigravityTranscriptReader _transcriptReader;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _activityThreshold;

    /// <inheritdoc />
    public string ProviderId => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityActivityMonitor"/> class.
    /// </summary>
    /// <param name="discovery">Optional endpoint discovery instance.</param>
    /// <param name="transcriptReader">Optional transcript reader instance.</param>
    /// <param name="timeProvider">Optional time provider for testing.</param>
    /// <param name="activityThreshold">Optional activity threshold duration (defaults to 45s).</param>
    public AntigravityActivityMonitor(
        AntigravityEndpointDiscovery? discovery = null,
        AntigravityTranscriptReader? transcriptReader = null,
        TimeProvider? timeProvider = null,
        TimeSpan? activityThreshold = null)
    {
        _discovery = discovery ?? new AntigravityEndpointDiscovery();
        _transcriptReader = transcriptReader ?? new AntigravityTranscriptReader();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _activityThreshold = activityThreshold ?? DefaultActivityThreshold;
    }

    /// <inheritdoc />
    public ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var endpoint = _discovery.DiscoverEndpoint();
        var latestWriteTime = _transcriptReader.GetLatestTranscriptWriteTimeUtc();

        if (endpoint is null && latestWriteTime is null)
        {
            return ValueTask.FromResult<AgentSession?>(null);
        }

        var now = _timeProvider.GetUtcNow();
        int pid = endpoint?.ProcessId ?? 0;
        var lastActivity = latestWriteTime ?? now;
        var elapsed = now - lastActivity;

        var state = elapsed <= _activityThreshold
            ? AgentSessionState.Busy
            : AgentSessionState.Idle;

        var session = new AgentSession
        {
            Pid = pid,
            StartTimeUtc = lastActivity,
            State = state,
            LastActivityUtc = lastActivity
        };

        return ValueTask.FromResult<AgentSession?>(session);
    }
}
