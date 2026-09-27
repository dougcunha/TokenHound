using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presents every registered and enabled provider, grouped by family, for the provider status window,
/// kept current by store events and a one-minute countdown tick.
/// </summary>
public sealed class ProviderStatusViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly TimeSpan REFRESH_INTERVAL = TimeSpan.FromMinutes(1);

    private readonly UsageStore _usageStore;
    private readonly Action<Action> _uiDispatcher;
    private readonly ProviderStatusFormatter _formatter;
    private readonly ITimer _timer;
    private volatile bool _disposed;
    private bool _isEmpty = true;

    /// <summary>Initializes a new instance of the <see cref="ProviderStatusViewModel"/> class.</summary>
    /// <param name="usageStore">The central usage store coordinator.</param>
    /// <param name="uiDispatcher">UI thread dispatcher action, or null to run synchronously.</param>
    /// <param name="formatter">Formatter owning the clock and culture, or null for system defaults.</param>
    public ProviderStatusViewModel(
        UsageStore usageStore,
        Action<Action>? uiDispatcher = null,
        ProviderStatusFormatter? formatter = null)
    {

        ArgumentNullException.ThrowIfNull(usageStore);

        _usageStore = usageStore;
        _uiDispatcher = uiDispatcher ?? (static action => action());
        _formatter = formatter ?? new ProviderStatusFormatter();

        _usageStore.SnapshotUpdated += OnSnapshotUpdated;
        _usageStore.ProviderEnablementChanged += OnProviderEnablementChanged;
        _timer = _formatter.TimeProvider.CreateTimer(
            OnTimerTick,
            null,
            REFRESH_INTERVAL,
            REFRESH_INTERVAL
        );

        Rebuild();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the provider family groups, ordered by family name.</summary>
    public ObservableCollection<ProviderStatusGroup> Groups { get; } = [];

    /// <summary>Gets a value indicating whether no provider is enabled.</summary>
    public bool IsEmpty
    {
        get
            => _isEmpty;
        private set
        {

            if (_isEmpty == value)
                return;

            _isEmpty = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _usageStore.SnapshotUpdated -= OnSnapshotUpdated;
        _usageStore.ProviderEnablementChanged -= OnProviderEnablementChanged;
        _timer.Dispose();
    }

    private void OnSnapshotUpdated(object? sender, Snapshot snapshot)
        => DispatchRebuild();

    private void OnProviderEnablementChanged(object? sender, ProviderEnablementChangedEventArgs args)
        => DispatchRebuild();

    private void OnTimerTick(object? state)
        => DispatchRebuild();

    private void DispatchRebuild()
    {

        if (_disposed)
            return;

        _uiDispatcher(Rebuild);
    }

    private void Rebuild()
    {

        if (_disposed)
            return;

        var snapshots = _usageStore.CurrentSnapshots;
        var accounts = _usageStore.RegisteredProviderIds
            .Where(_usageStore.IsProviderEnabled)
            .Select(id => ProviderStatusProjection.CreateAccount(id, snapshots.GetValueOrDefault(id), _formatter));

        Groups.Clear();

        foreach (var group in ProviderStatusProjection.Group(accounts))
            Groups.Add(group);

        IsEmpty = Groups.Count == 0;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
