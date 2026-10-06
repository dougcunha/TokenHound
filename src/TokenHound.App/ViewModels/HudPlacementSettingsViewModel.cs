using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.ViewModels;

/// <summary>
/// General tab "HUD placement" section: chooses the placement mode and the preferred display and applies each choice
/// immediately through <see cref="HudPlacementService"/>. The display list is captured when Settings opens.
/// </summary>
public sealed class HudPlacementSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Error shown when a placement choice could not be saved.</summary>
    public const string SAVE_ERROR = "Failed to save the HUD placement.";

    /// <summary>Hint shown in Free mode, where the display selector is disabled.</summary>
    public const string FREE_HINT = "In Free mode the HUD stays on the display where you drag it.";

    /// <summary>Label of the option that follows the Windows primary display.</summary>
    public const string PRIMARY_LABEL = "Primary monitor";

    /// <summary>Suffix of a stored preferred display that is not connected.</summary>
    public const string DISCONNECTED_SUFFIX = " (disconnected — showing on primary)";

    private static readonly IReadOnlyList<ModeOption> MODES =
    [
        new(HudDockMode.TopLeft, "Top left"),
        new(HudDockMode.TopCenter, "Top center"),
        new(HudDockMode.TopRight, "Top right"),
        new(HudDockMode.LeftEdge, "Left edge"),
        new(HudDockMode.RightEdge, "Right edge"),
        new(HudDockMode.Free, "Free")
    ];

    private readonly Func<HudPlacementContext> _context;
    private readonly HudPlacementService _service;

    private ModeOption _selectedMode;
    private DisplayOption _selectedDisplay;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPlacementSettingsViewModel"/> class.
    /// </summary>
    /// <param name="service">The placement service applying and persisting choices.</param>
    /// <param name="displays">The displays connected when Settings opened.</param>
    /// <param name="context">Supplies the HUD window's current displays, hosting display, and position.</param>
    public HudPlacementSettingsViewModel(
        HudPlacementService service,
        IReadOnlyList<DisplayInfo> displays,
        Func<HudPlacementContext> context
    )
    {

        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(context);

        _service = service;
        _context = context;
        Displays = BuildDisplayOptions(service.Current.Display, displays);
        _selectedMode = MODES.First(option => option.Mode == service.Mode);
        _selectedDisplay = FindDisplayOption(service.Current.Display, displays);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the selectable placement modes.</summary>
    public IReadOnlyList<ModeOption> Modes
        => MODES;

    /// <summary>Gets the selectable displays: the primary option, each connected display, and a disconnected preference.</summary>
    public IReadOnlyList<DisplayOption> Displays { get; }

    /// <summary>Gets or sets the placement mode; setting it applies the mode immediately.</summary>
    public ModeOption SelectedMode
    {
        get => _selectedMode;
        set
        {

            if (value is null || value == _selectedMode)
                return;

            _selectedMode = value;
            Report(_service.SelectMode(value.Mode, _context()));
            SyncDisplayWithService();
            Notify(nameof(SelectedMode), nameof(IsDisplayEnabled), nameof(DisplayHint));
        }
    }

    /// <summary>Gets or sets the preferred display option; setting it applies the display immediately.</summary>
    public DisplayOption SelectedDisplay
    {
        get => _selectedDisplay;
        set
        {

            if (value is null || value == _selectedDisplay)
                return;

            _selectedDisplay = value;
            Report(_service.SelectDisplay(value.Preference));
            Notify(nameof(SelectedDisplay));
        }
    }

    /// <summary>Gets a value indicating whether the display selector applies, which is every mode except Free.</summary>
    public bool IsDisplayEnabled
        => _selectedMode.Mode != HudDockMode.Free;

    /// <summary>Gets the hint shown next to the display selector, or <see langword="null"/> when none applies.</summary>
    public string? DisplayHint
        => IsDisplayEnabled ? null : FREE_HINT;

    /// <summary>Gets the error of the most recent failed save, or <see langword="null"/>.</summary>
    public string? SaveError { get; private set; }

    /// <summary>
    /// Creates the section from the live displays and the HUD window context.
    /// </summary>
    /// <param name="service">The placement service.</param>
    /// <param name="getDisplays">Lists the connected displays.</param>
    /// <param name="context">Supplies the HUD window placement context.</param>
    /// <returns>The section model.</returns>
    public static HudPlacementSettingsViewModel Create(
        HudPlacementService service,
        Func<IReadOnlyList<DisplayInfo>> getDisplays,
        Func<HudPlacementContext> context
    )
    {

        ArgumentNullException.ThrowIfNull(getDisplays);

        return new HudPlacementSettingsViewModel(service, getDisplays(), context);
    }

    /// <summary>
    /// Formats the label of a connected display, such as <c>2 — DELL U2723QE · 3840×2160 · Primary</c>.
    /// </summary>
    /// <param name="display">The display.</param>
    /// <returns>The label.</returns>
    public static string FormatDisplay(DisplayInfo display)
    {

        ArgumentNullException.ThrowIfNull(display);

        var primary = display.IsPrimary ? " · Primary" : string.Empty;

        return $"{display.Number} — {display.Name} · {display.Width}×{display.Height}{primary}";
    }

    private static List<DisplayOption> BuildDisplayOptions(HudDisplayPreference? stored, IReadOnlyList<DisplayInfo> displays)
    {

        List<DisplayOption> options = [new DisplayOption(PRIMARY_LABEL, null, IsDisconnected: false)];
        options.AddRange(displays.Select(static display => new DisplayOption(
            FormatDisplay(display),
            DisplayResolver.ToPreference(display),
            IsDisconnected: false
        )));

        var isMissing = displays.Count == 0 || DisplayResolver.Resolve(stored, displays).IsFallback;

        if (!DisplayResolver.IsPrimaryPreference(stored) && isMissing)
            options.Add(new DisplayOption($"{stored!.Name ?? "Display"}{DISCONNECTED_SUFFIX}", stored, IsDisconnected: true));

        return options;
    }

    private DisplayOption FindDisplayOption(HudDisplayPreference? stored, IReadOnlyList<DisplayInfo> displays)
    {

        if (DisplayResolver.IsPrimaryPreference(stored))
            return Displays[0];

        var disconnected = Displays.FirstOrDefault(static option => option.IsDisconnected);

        if (disconnected is not null)
            return disconnected;

        var target = DisplayResolver.Resolve(stored, displays).Target;

        return Displays.First(option => string.Equals(option.Preference?.DevicePath, target.DevicePath, StringComparison.OrdinalIgnoreCase));
    }

    // Leaving Free can make the hosting display the preference (FR-18); reflect it without saving again.
    private void SyncDisplayWithService()
    {

        var stored = _service.Current.Display;

        if (DisplayResolver.IsPrimaryPreference(stored) || _selectedDisplay.Preference == stored)
            return;

        var match = Displays.FirstOrDefault(option => string.Equals(option.Preference?.DevicePath, stored!.DevicePath, StringComparison.OrdinalIgnoreCase));

        if (match is null || match == _selectedDisplay)
            return;

        _selectedDisplay = match;
        Notify(nameof(SelectedDisplay));
    }

    private void Report(bool saved)
    {

        SaveError = saved ? null : SAVE_ERROR;
        Notify(nameof(SaveError));
    }

    private void Notify(params string[] propertyNames)
    {

        foreach (var name in propertyNames)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// A selectable placement mode.
    /// </summary>
    /// <param name="Mode">The placement mode.</param>
    /// <param name="Label">The label shown in Settings.</param>
    public sealed record ModeOption(HudDockMode Mode, string Label);

    /// <summary>
    /// A selectable display preference.
    /// </summary>
    /// <param name="Label">The label shown in Settings.</param>
    /// <param name="Preference">The preference stored when selected; <see langword="null"/> follows the primary display.</param>
    /// <param name="IsDisconnected">Whether this is a stored preference that is not connected.</param>
    public sealed record DisplayOption(string Label, HudDisplayPreference? Preference, bool IsDisconnected);
}
