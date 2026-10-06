using System;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted screen placement of the HUD capsule: placement mode, preferred display, and free-mode coordinates.
/// </summary>
public sealed record HudPositionSettings
{
    /// <summary>
    /// Gets the horizontal offset of the HUD window in device-independent pixels, or <see langword="null"/> when never persisted.
    /// </summary>
    public double? Left { get; init; }

    /// <summary>
    /// Gets the vertical offset of the HUD window in device-independent pixels, or <see langword="null"/> when never persisted.
    /// </summary>
    public double? Top { get; init; }

    /// <summary>
    /// Gets the stored placement mode name, or <see langword="null"/> when written by a version without docking.
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    /// Gets the preferred display for docked modes, or <see langword="null"/> to follow the primary display.
    /// </summary>
    public HudDisplayPreference? Display { get; init; }

    /// <summary>
    /// Attempts to read a usable placement from the persisted coordinates.
    /// </summary>
    /// <param name="left">Receives the horizontal offset when available.</param>
    /// <param name="top">Receives the vertical offset when available.</param>
    /// <returns><see langword="true"/> when both coordinates are present and finite.</returns>
    public bool TryGetPosition(out double left, out double top)
    {

        left = Left ?? double.NaN;
        top = Top ?? double.NaN;

        return double.IsFinite(left) && double.IsFinite(top);
    }

    /// <summary>
    /// Resolves the effective placement mode, migrating coordinate-only settings to Free and falling back to Top Center.
    /// </summary>
    /// <returns>The effective mode and, when the stored values could not be honored, a warning describing why.</returns>
    public (HudDockMode Mode, string? Warning) ResolveMode()
    {

        var hasPosition = TryGetPosition(out _, out _);

        if (Mode is null)
            return (hasPosition ? HudDockMode.Free : HudDockMode.TopCenter, null);

        if (!TryParseMode(Mode, out var mode))
            return (HudDockMode.TopCenter, $"Unknown HUD placement {nameof(Mode)} '{Mode}'; using {HudDockMode.TopCenter}.");

        if (mode == HudDockMode.Free && !hasPosition)
            return (HudDockMode.TopCenter, $"HUD placement {nameof(Mode)} '{Mode}' has no stored position; using {HudDockMode.TopCenter}.");

        return (mode, null);
    }

    private static bool TryParseMode(string value, out HudDockMode mode)
    {

        foreach (var candidate in Enum.GetValues<HudDockMode>())
        {

            if (string.Equals(value, candidate.ToString(), StringComparison.OrdinalIgnoreCase))
            {

                mode = candidate;

                return true;
            }
        }

        mode = HudDockMode.TopCenter;

        return false;
    }
}
