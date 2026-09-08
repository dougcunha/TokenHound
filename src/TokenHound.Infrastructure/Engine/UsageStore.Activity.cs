using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageStore
{
    private async Task PollActivityAsync(CancellationToken cancellationToken)
    {

        using var lease = _lifetime.Enter(cancellationToken);
        IActivityMonitor[] monitors;

        lock (_monitorsLock)
            monitors = [.. _monitors];

        foreach (var monitor in monitors)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (!IsProviderEnabled(monitor.ProviderId))
                continue;

            var session = await CheckActivityAsync(monitor, cancellationToken).ConfigureAwait(false);
            PublishActivity(monitor.ProviderId, session);
        }
    }

    private void PublishActivity(string providerId, AgentSession? session)
    {

        var changed = false;

        lock (_activityLock)
        {

            if (!_activityStates.TryGetValue(providerId, out var previous)
                || !Equals(previous, session))
            {
                _activityStates[providerId] = session;
                changed = true;
            }
        }

        if (changed)
            ActivityUpdated?.Invoke(this, new ProviderActivityChangedEventArgs(providerId, session));
    }

    private void ClearActivityState(string providerId)
    {

        lock (_activityLock)
            _activityStates.Remove(providerId);

        ActivityUpdated?.Invoke(this, new ProviderActivityChangedEventArgs(providerId, null));
    }

    private bool HasActivityState
    {
        get
        {

            lock (_activityLock)
                return _activityStates.Count > 0;
        }
    }

    private bool HasBusyActivity
    {
        get
        {

            lock (_activityLock)
            {

                foreach (var session in _activityStates.Values)
                {

                    if (session?.State == AgentSessionState.Busy)
                        return true;
                }

                return false;
            }
        }
    }

    private static async ValueTask<AgentSession?> CheckActivityAsync(
        IActivityMonitor monitor,
        CancellationToken cancellationToken)
    {

        try
        {

            return await monitor.CheckLivenessAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch
        {

            return null;
        }
    }

    private async ValueTask<bool> CheckAnyBusyAsync(CancellationToken cancellationToken)
    {

        IActivityMonitor[] monitors;

        lock (_monitorsLock)
            monitors = [.. _monitors];

        foreach (var monitor in monitors)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (!IsProviderEnabled(monitor.ProviderId))
                continue;

            if (await IsBusyAsync(monitor, cancellationToken).ConfigureAwait(false))
                return true;
        }

        return false;
    }

    private static async ValueTask<bool> IsBusyAsync(IActivityMonitor monitor, CancellationToken cancellationToken)
    {

        try
        {

            var session = await monitor.CheckLivenessAsync(cancellationToken).ConfigureAwait(false);

            return session?.State == AgentSessionState.Busy;
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch
        {

            return false;
        }
    }
}
