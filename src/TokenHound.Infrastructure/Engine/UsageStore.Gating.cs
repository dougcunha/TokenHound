using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageStore
{
    private readonly ConcurrentDictionary<string, bool> _enablements = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Occurs whenever the monitoring state of a registered provider actually changes.</summary>
    public event EventHandler<ProviderEnablementChangedEventArgs>? ProviderEnablementChanged;

    /// <summary>
    /// Gets the identifiers of every registered provider. Ordering is not guaranteed.
    /// </summary>
    public IReadOnlyCollection<string> RegisteredProviderIds
        => [.. _providers.Keys];

    /// <summary>
    /// Determines whether a provider is currently monitored. An unknown identifier is reported as monitored.
    /// </summary>
    /// <param name="providerId">The provider identifier to evaluate.</param>
    /// <returns><see langword="true"/> when the provider is monitored; otherwise, <see langword="false"/>.</returns>
    public bool IsProviderEnabled(string providerId)
        => !_enablements.TryGetValue(providerId, out var isEnabled) || isEnabled;

    /// <summary>
    /// Enables or disables monitoring for a registered provider. The call is idempotent, is a no-op for an
    /// unregistered identifier, and raises <see cref="ProviderEnablementChanged"/> only on an actual change.
    /// Disabling also clears the provider's activity state so it no longer forces the active cadence.
    /// </summary>
    /// <param name="providerId">The provider identifier to gate.</param>
    /// <param name="isEnabled">Whether the provider should be monitored.</param>
    public void SetProviderEnabled(string providerId, bool isEnabled)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        if (!_providers.ContainsKey(providerId))
            return;

        var wasEnabled = IsProviderEnabled(providerId);
        _enablements[providerId] = isEnabled;

        if (wasEnabled == isEnabled)
            return;

        if (!isEnabled)
            ClearActivityState(providerId);

        Log.Information(
            "Provider {ProviderId} monitoring changed to {IsEnabled}",
            providerId,
            isEnabled
        );

        ProviderEnablementChanged?.Invoke(this, new ProviderEnablementChangedEventArgs(providerId, isEnabled));
    }

    /// <summary>
    /// Forces an immediate refresh of a single registered provider, bypassing schedule checks but still honoring
    /// any unexpired rate-limit deadline. The call is a no-op for an unknown or disabled provider identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier to refresh.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshProviderNowAsync(string providerId, CancellationToken cancellationToken = default)
    {

        ThrowIfDisposedOrStopping();
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_providers.TryGetValue(providerId, out var provider) || !IsProviderEnabled(providerId))
            return;

        using var lease = _lifetime.Enter(cancellationToken);

        await _refreshLock.WaitAsync(lease.Token).ConfigureAwait(false);

        try
        {

            await RefreshProviderAsync(provider, lease.Token).ConfigureAwait(false);
        }
        finally
        {

            _refreshLock.Release();
        }
    }
}
