using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    /// <summary>Loads Copilot HTTP rate-limit state, including legacy deadlines.</summary>
    public CopilotHttpGateState LoadCopilotHttpDeadline()
        => _copilotHttpArchive.Load();

    /// <summary>Persists the Copilot HTTP deadline and consecutive failure count.</summary>
    public Task SaveCopilotHttpDeadlineAsync(
        DateTimeOffset deadlineUtc,
        int consecutiveFailures,
        CancellationToken cancellationToken = default)
        => _copilotHttpArchive.SaveAsync(
            deadlineUtc,
            consecutiveFailures,
            cancellationToken
        );

    /// <summary>Clears the Copilot HTTP deadline and failure count.</summary>
    public Task ClearCopilotHttpDeadlineAsync(CancellationToken cancellationToken = default)
        => _copilotHttpArchive.ClearAsync(cancellationToken);
}
