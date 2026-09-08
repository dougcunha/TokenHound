using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Claude;
using TokenHound.Infrastructure.Providers.Mock;

namespace TokenHound.App.ViewModels;

/// <summary>Represents the view model for the Notch HUD capsule, coordinating rings and fallback logic.</summary>
public sealed class NotchViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly UsageStore _usageStore;
    private readonly Action<Action> _uiDispatcher;
    private readonly Dictionary<string, ProviderRingViewModel> _ringsByProvider = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _ringOrder = [];
    private MockUsageProvider? _mockProvider;
    private bool _isFallbackActive;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="NotchViewModel"/> class.</summary>
    /// <param name="usageStore">The central usage store coordinator.</param>
    /// <param name="uiDispatcher">Optional UI thread dispatcher action, or null for synchronous.</param>
    /// <param name="mockProvider">Optional mock provider for offline fallback, or null if unconfigured.</param>
    public NotchViewModel(
        UsageStore usageStore,
        Action<Action>? uiDispatcher = null,
        MockUsageProvider? mockProvider = null)
    {

        ArgumentNullException.ThrowIfNull(usageStore);

        _usageStore = usageStore;
        _uiDispatcher = uiDispatcher ?? (static action => action());
        _mockProvider = mockProvider;

        _usageStore.SnapshotUpdated += OnSnapshotUpdated;
        _usageStore.ActivityUpdated += OnActivityUpdated;
        _usageStore.ProviderEnablementChanged += OnProviderEnablementChanged;

        foreach (var snapshot in _usageStore.CurrentSnapshots.Values)
        {
            UpdateOrAddRing(snapshot);
        }

        if (_mockProvider is not null && (Rings.Count == 0 || ShouldFallbackToMock()))
            LoadMockFallback();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the collection of active provider ring view models.</summary>
    public ObservableCollection<ProviderRingViewModel> Rings { get; } = [];

    /// <summary>Gets a value indicating whether the mock fallback provider is currently active.</summary>
    public bool IsFallbackActive
        => _isFallbackActive;

    /// <summary>Activates and registers the fallback mock provider to ensure HUD visibility.</summary>
    /// <param name="provider">The mock provider to register and load, or null for default/configured.</param>
    public void LoadMockFallback(MockUsageProvider? provider = null)
    {

        if (_isFallbackActive)
            return;

        var mock = provider ?? _mockProvider ?? new MockUsageProvider();
        _mockProvider = mock;
        _isFallbackActive = true;
        OnPropertyChanged(nameof(IsFallbackActive));

        _usageStore.RegisterProvider(mock);

        var snapshotTask = mock.GetSnapshotAsync();
        var snapshot = snapshotTask.IsCompletedSuccessfully
            ? snapshotTask.Result
            : snapshotTask.AsTask().GetAwaiter().GetResult();

        _uiDispatcher(() => UpdateOrAddRing(snapshot));
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _usageStore.SnapshotUpdated -= OnSnapshotUpdated;
        _usageStore.ActivityUpdated -= OnActivityUpdated;
        _usageStore.ProviderEnablementChanged -= OnProviderEnablementChanged;
    }

    private void OnSnapshotUpdated(object? sender, Snapshot snapshot)
    {

        _uiDispatcher(() =>
        {

            UpdateOrAddRing(snapshot);

            if (_mockProvider is not null && snapshot.Status == ProviderStatus.NeedsAuth && !_isFallbackActive)
                LoadMockFallback();
        });
    }

    private void OnActivityUpdated(
        object? sender,
        ProviderActivityChangedEventArgs args)
    {
        _uiDispatcher(() =>
        {
            var ring = Rings.FirstOrDefault(r =>
                string.Equals(r.ProviderId, args.ProviderId, StringComparison.OrdinalIgnoreCase));

            ring?.UpdateActivity(args.AgentSession);
        });
    }

    private void OnProviderEnablementChanged(
        object? sender,
        ProviderEnablementChangedEventArgs args)
        => _uiDispatcher(() => ApplyEnablement(args.ProviderId, args.IsEnabled));

    private void ApplyEnablement(string providerId, bool isEnabled)
    {

        if (!_ringsByProvider.TryGetValue(providerId, out var ring))
        {

            if (isEnabled && _usageStore.CurrentSnapshots.TryGetValue(providerId, out var restored))
                UpdateOrAddRing(restored);

            return;
        }

        if (!isEnabled)
        {

            Rings.Remove(ring);

            return;
        }

        if (Rings.Contains(ring))
            return;

        if (_usageStore.CurrentSnapshots.TryGetValue(providerId, out var cached))
            ring.UpdateFromSnapshot(cached);

        Rings.Insert(ResolveInsertIndex(providerId), ring);
    }

    private int ResolveInsertIndex(string providerId)
    {

        var index = 0;

        foreach (var knownId in _ringOrder)
        {

            if (string.Equals(knownId, providerId, StringComparison.OrdinalIgnoreCase))
                return index;

            if (_ringsByProvider.TryGetValue(knownId, out var known) && Rings.Contains(known))
                index++;
        }

        return index;
    }

    private void UpdateOrAddRing(Snapshot snapshot)
    {

        var isEnabled = _usageStore.IsProviderEnabled(snapshot.ProviderId);

        if (_ringsByProvider.TryGetValue(snapshot.ProviderId, out var existing))
        {

            existing.UpdateFromSnapshot(snapshot);

            if (isEnabled && !Rings.Contains(existing))
                Rings.Insert(ResolveInsertIndex(snapshot.ProviderId), existing);

            return;
        }

        var ring = new ProviderRingViewModel(snapshot.ProviderId);
        ring.UpdateFromSnapshot(snapshot);
        _ringsByProvider[snapshot.ProviderId] = ring;
        _ringOrder.Add(snapshot.ProviderId);

        if (isEnabled)
            Rings.Insert(ResolveInsertIndex(snapshot.ProviderId), ring);
    }

    private bool ShouldFallbackToMock()
    {

        if (Rings.Count == 0)
            return true;

        if (Rings.Any(static r => string.Equals(r.ProviderId, "mock", StringComparison.OrdinalIgnoreCase)))
            return false;

        var claudeRing = Rings.FirstOrDefault(static r =>
            string.Equals(r.ProviderId, ClaudeOAuthProvider.PROVIDER_ID, StringComparison.OrdinalIgnoreCase));

        if (claudeRing is not null && claudeRing.Status == ProviderStatus.NeedsAuth)
            return true;

        if (claudeRing is null && !HasClaudeCredentials())
            return true;

        return false;
    }

    private static bool HasClaudeCredentials()
    {

        try
        {

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrWhiteSpace(userProfile))
                return false;

            var defaultPath = Path.Combine(userProfile, ".claude", ".credentials.json");

            if (File.Exists(defaultPath))
                return true;

            if (Directory.Exists(userProfile))
            {

                var multiDirs = Directory.GetDirectories(userProfile, ".claude-*");

                foreach (var dir in multiDirs)
                {

                    if (File.Exists(Path.Combine(dir, ".credentials.json")))
                        return true;
                }
            }

            return false;
        }
        catch
        {

            return false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
