using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents a single provider row of the Settings dialog: provider identity, catalog display metadata,
/// the live status badge, and the two-way monitoring toggle that drives the engine gate.
/// </summary>
/// <remarks>
/// The view model stays free of presentation framework types so it compiles into the <c>net10.0</c>
/// test project. Badge brushes are exposed as resource key names, never as brush instances.
/// </remarks>
public sealed class ProviderToggleViewModel : INotifyPropertyChanged
{
    private readonly string _providerId;
    private readonly string _providerName;
    private readonly string _providerBadge;
    private readonly string? _glyphKey;
    private readonly double _glyphScale;
    private readonly Action<string, bool> _monitoringChanged;
    private ProviderBadgeState _badgeState;
    private bool _isMonitored;

    /// <summary>Initializes a new instance of the <see cref="ProviderToggleViewModel"/> class.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <param name="isMonitored">The initial monitoring state resolved from the engine.</param>
    /// <param name="badgeState">The initial status badge state.</param>
    /// <param name="monitoringChanged">Callback invoked with the provider identifier and the new state whenever the user toggles the row.</param>
    public ProviderToggleViewModel(
        string providerId,
        bool isMonitored,
        ProviderBadgeState badgeState,
        Action<string, bool> monitoringChanged)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(monitoringChanged);

        _providerId = providerId;
        _providerName = ProviderCatalog.ResolveDefaultName(providerId);
        _providerBadge = ProviderCatalog.ResolveDefaultBadge(providerId);
        _glyphKey = ProviderCatalog.ResolveGlyphKey(providerId);
        _glyphScale = ProviderCatalog.ResolveGlyphScale(providerId);
        _monitoringChanged = monitoringChanged;
        _isMonitored = isMonitored;
        _badgeState = badgeState;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the unique identifier of the provider.</summary>
    public string ProviderId
        => _providerId;

    /// <summary>Gets the human-readable display name resolved from the provider catalog.</summary>
    public string ProviderName
        => _providerName;

    /// <summary>Gets the short text badge used when no vector mark is available.</summary>
    public string ProviderBadge
        => _providerBadge;

    /// <summary>Gets the resource key of the provider vector mark, or <see langword="null"/> to display the text badge.</summary>
    public string? GlyphKey
        => _glyphKey;

    /// <summary>Gets the optical size multiplier applied to the provider vector mark.</summary>
    public double GlyphScale
        => _glyphScale;

    /// <summary>Gets the current status badge state of the row.</summary>
    public ProviderBadgeState BadgeState
        => _badgeState;

    /// <summary>Gets the visible text of the status badge, so the state never relies on color alone.</summary>
    public string BadgeLabel
        => ProviderBadgeResolver.ResolveLabel(_badgeState);

    /// <summary>Gets the resource key of the status badge background brush.</summary>
    public string BadgeBackgroundKey
        => ProviderBadgeResolver.ResolveBackgroundKey(_badgeState);

    /// <summary>Gets the resource key of the status badge foreground brush.</summary>
    public string BadgeForegroundKey
        => ProviderBadgeResolver.ResolveForegroundKey(_badgeState);

    /// <summary>
    /// Gets or sets a value indicating whether the provider is monitored. Setting the property notifies the
    /// owner, which gates the engine, refreshes the badge, and persists the preference without blocking.
    /// </summary>
    public bool IsMonitored
    {
        get
            => _isMonitored;
        set
        {

            if (_isMonitored == value)
                return;

            _isMonitored = value;
            OnPropertyChanged();
            _monitoringChanged(_providerId, value);
        }
    }

    /// <summary>Applies a newly resolved badge state and notifies every dependent badge property.</summary>
    /// <param name="badgeState">The badge state to display.</param>
    public void ApplyBadgeState(ProviderBadgeState badgeState)
    {

        if (_badgeState == badgeState)
            return;

        _badgeState = badgeState;
        OnPropertyChanged(nameof(BadgeState));
        OnPropertyChanged(nameof(BadgeLabel));
        OnPropertyChanged(nameof(BadgeBackgroundKey));
        OnPropertyChanged(nameof(BadgeForegroundKey));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
