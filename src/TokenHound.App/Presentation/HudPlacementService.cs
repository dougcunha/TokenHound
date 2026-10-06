using Serilog;
using System;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.Presentation;

/// <summary>
/// Owns the HUD placement state and its persistence so the drag gesture, the context menu, and Settings change the same state.
/// Every write derives from the current settings, so no stored field is lost.
/// </summary>
public sealed class HudPlacementService
{
    private static readonly ILogger LOGGER = Log.ForContext<HudPlacementService>();

    private readonly Func<HudPositionSettings, bool> _save;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPlacementService"/> class.
    /// </summary>
    /// <param name="initial">The placement loaded at startup.</param>
    /// <param name="save">Persists a placement and reports whether the write succeeded.</param>
    public HudPlacementService(HudPositionSettings initial, Func<HudPositionSettings, bool> save)
    {

        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(save);

        Current = initial;
        _save = save;
    }

    /// <summary>
    /// Occurs after the placement changed, whether or not it could be persisted.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets the current placement settings.
    /// </summary>
    public HudPositionSettings Current { get; private set; }

    /// <summary>
    /// Gets the effective placement mode of <see cref="Current"/>.
    /// </summary>
    public HudDockMode Mode
        => Current.ResolveMode().Mode;

    /// <summary>
    /// Creates a service seeded from and persisting to the specified store.
    /// </summary>
    /// <param name="store">The HUD placement store.</param>
    /// <returns>The service holding the stored placement.</returns>
    public static HudPlacementService Create(HudPositionStore store)
    {

        ArgumentNullException.ThrowIfNull(store);

        return new HudPlacementService(store.Load(), store.Save);
    }

    /// <summary>
    /// Selects a placement mode. Free stores the current window position; leaving Free for a docked mode keeps the HUD
    /// on the display hosting it, which becomes the preferred display when it differs from the resolved one.
    /// </summary>
    /// <param name="mode">The mode to apply.</param>
    /// <param name="context">The displays, hosting display, and window position at the time of the choice.</param>
    /// <returns><see langword="false"/> only when the change could not be persisted.</returns>
    public bool SelectMode(HudDockMode mode, HudPlacementContext context)
    {

        ArgumentNullException.ThrowIfNull(context);

        if (mode == HudDockMode.Free)
            return Apply(Current with { Mode = nameof(HudDockMode.Free), Left = context.Left, Top = context.Top });

        if (string.Equals(Current.Mode, mode.ToString(), StringComparison.OrdinalIgnoreCase))
            return true;

        var display = Mode == HudDockMode.Free ? PreferHosting(context) : Current.Display;

        return Apply(Current with { Mode = mode.ToString(), Display = display });
    }

    /// <summary>
    /// Selects the preferred display for docked modes.
    /// </summary>
    /// <param name="preference">The display to prefer, or <see langword="null"/> to follow the primary display.</param>
    /// <returns><see langword="false"/> only when the change could not be persisted.</returns>
    public bool SelectDisplay(HudDisplayPreference? preference)
        => Apply(Current with { Display = preference });

    /// <summary>
    /// Records a completed drag: the HUD switches to Free at the dropped position.
    /// </summary>
    /// <param name="left">The dropped horizontal offset in device-independent pixels.</param>
    /// <param name="top">The dropped vertical offset in device-independent pixels.</param>
    /// <returns><see langword="false"/> only when the change could not be persisted.</returns>
    public bool RecordDrag(double left, double top)
        => Apply(Current with { Mode = nameof(HudDockMode.Free), Left = left, Top = top });

    /// <summary>
    /// Stores a corrected Free position after the HUD was clamped back onto a display, without changing the mode.
    /// </summary>
    /// <param name="left">The corrected horizontal offset in device-independent pixels.</param>
    /// <param name="top">The corrected vertical offset in device-independent pixels.</param>
    /// <returns><see langword="false"/> only when the change could not be persisted.</returns>
    public bool UpdateFreePosition(double left, double top)
        => Apply(Current with { Left = left, Top = top });

    private HudDisplayPreference? PreferHosting(HudPlacementContext context)
    {

        if (context.Hosting is null || context.Displays.Count == 0)
            return Current.Display;

        var resolved = DisplayResolver.Resolve(Current.Display, context.Displays).Target;

        return string.Equals(resolved.DevicePath, context.Hosting.DevicePath, StringComparison.OrdinalIgnoreCase)
            ? Current.Display
            : DisplayResolver.ToPreference(context.Hosting);
    }

    private bool Apply(HudPositionSettings next)
    {

        if (next == Current)
            return true;

        Current = next;
        var saved = _save(next);

        if (saved)
        {

            LOGGER.Debug(
                "Persisted HUD placement {Mode} on {Display} at {Left},{Top}",
                next.Mode,
                next.Display?.Name,
                next.Left,
                next.Top
            );
        }
        else
            LOGGER.Warning("Unable to persist HUD placement {Mode}", next.Mode);

        Changed?.Invoke(this, EventArgs.Empty);

        return saved;
    }
}
