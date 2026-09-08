using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents the Settings dialog: one toggle row per provider registered in the usage store, with live
/// status badges and immediate application of every toggle. There is no Apply or Save step.
/// </summary>
/// <remarks>
/// A toggle is applied in a fixed order — engine gate, badge refresh, then an untracked disk write — so the
/// visual response never waits on persistence. The view model holds no presentation framework types.
/// </remarks>
public sealed class SettingsViewModel : IDisposable
{
    private const string PERSIST_FAILED_MESSAGE = "Provider monitoring preferences were not persisted";
    private const string REFRESH_FAILED_MESSAGE = "Immediate refresh of re-enabled provider {ProviderId} failed";

    private readonly UsageStore _usageStore;
    private readonly Func<ProviderSettings, CancellationToken, Task<bool>> _persistAsync;
    private readonly Action<Action> _uiDispatcher;
    private readonly Dictionary<string, ProviderToggleViewModel> _rowsByProvider = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="SettingsViewModel"/> class.</summary>
    /// <param name="usageStore">The central usage store owning provider registrations and the enablement gate.</param>
    /// <param name="persistAsync">The asynchronous persistence operation for the full enablement map.</param>
    /// <param name="uiDispatcher">Optional UI thread dispatcher action, or <see langword="null"/> for synchronous execution.</param>
    public SettingsViewModel(
        UsageStore usageStore,
        Func<ProviderSettings, CancellationToken, Task<bool>> persistAsync,
        Action<Action>? uiDispatcher = null)
    {

        ArgumentNullException.ThrowIfNull(usageStore);
        ArgumentNullException.ThrowIfNull(persistAsync);

        _usageStore = usageStore;
        _persistAsync = persistAsync;
        _uiDispatcher = uiDispatcher ?? (static action => action());

        BuildRows();

        _usageStore.SnapshotUpdated += OnSnapshotUpdated;
    }

    /// <summary>Gets the provider rows, ordered by display name so the dialog is stable across launches.</summary>
    public ObservableCollection<ProviderToggleViewModel> Providers { get; } = [];

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _usageStore.SnapshotUpdated -= OnSnapshotUpdated;
    }

    private void BuildRows()
    {

        var snapshots = _usageStore.CurrentSnapshots;

        var orderedIds = _usageStore
            .RegisteredProviderIds
            .OrderBy(ProviderCatalog.ResolveDefaultName, StringComparer.OrdinalIgnoreCase);

        foreach (var providerId in orderedIds)
        {

            var isMonitored = _usageStore.IsProviderEnabled(providerId);
            snapshots.TryGetValue(providerId, out var snapshot);

            var row = new ProviderToggleViewModel(
                providerId,
                isMonitored,
                ProviderBadgeResolver.ResolveState(isMonitored, snapshot),
                OnMonitoringChanged
            );

            _rowsByProvider[providerId] = row;
            Providers.Add(row);
        }
    }

    private void OnMonitoringChanged(string providerId, bool isMonitored)
    {

        _usageStore.SetProviderEnabled(providerId, isMonitored);
        RefreshBadge(providerId);

        _ = PersistAsync(BuildSettings());

        if (isMonitored)
            _ = RefreshProviderAsync(providerId);
    }

    private void OnSnapshotUpdated(object? sender, Snapshot snapshot)
        => _uiDispatcher(() => RefreshBadge(snapshot.ProviderId));

    private void RefreshBadge(string providerId)
    {

        if (!_rowsByProvider.TryGetValue(providerId, out var row))
            return;

        _usageStore.CurrentSnapshots.TryGetValue(providerId, out var snapshot);

        var isMonitored = _usageStore.IsProviderEnabled(providerId);

        row.ApplyBadgeState(ProviderBadgeResolver.ResolveState(isMonitored, snapshot));
    }

    private ProviderSettings BuildSettings()
    {

        var states = new Dictionary<string, bool>(Providers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var row in Providers)
            states[row.ProviderId] = _usageStore.IsProviderEnabled(row.ProviderId);

        return new ProviderSettings { EnabledStates = states };
    }

    private async Task PersistAsync(ProviderSettings settings)
    {

        try
        {

            var persisted = await _persistAsync(settings, CancellationToken.None).ConfigureAwait(false);

            if (!persisted)
                Log.Warning(PERSIST_FAILED_MESSAGE);
        }
        catch (Exception exception)
        {

            Log.Warning(exception, PERSIST_FAILED_MESSAGE);
        }
    }

    private async Task RefreshProviderAsync(string providerId)
    {

        try
        {

            await _usageStore.RefreshProviderNowAsync(providerId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {

            Log.Warning(exception, REFRESH_FAILED_MESSAGE, providerId);
        }
    }
}
