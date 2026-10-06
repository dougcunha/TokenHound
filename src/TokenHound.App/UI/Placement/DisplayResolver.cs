using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Placement;

/// <summary>
/// Matches the persisted display preference against the connected displays.
/// </summary>
public static class DisplayResolver
{
    /// <summary>
    /// Resolves the display hosting the docked HUD: the preferred display when connected, otherwise the primary display.
    /// </summary>
    /// <param name="preference">The persisted preference, or <see langword="null"/> to follow the primary display.</param>
    /// <param name="displays">The connected displays; must not be empty.</param>
    /// <returns>The hosting display and whether it is a fallback for a missing preferred display.</returns>
    public static DisplayResolution Resolve(HudDisplayPreference? preference, IReadOnlyList<DisplayInfo> displays)
    {

        ArgumentNullException.ThrowIfNull(displays);

        if (displays.Count == 0)
            throw new ArgumentException("At least one display is required.", nameof(displays));

        var primary = displays.FirstOrDefault(static display => display.IsPrimary) ?? displays[0];

        if (IsPrimaryPreference(preference))
            return new() { Target = primary };

        var match = FindMatch(preference!, displays);

        return match is null
            ? new() { Target = primary, IsFallback = true }
            : new() { Target = match };
    }

    /// <summary>
    /// Creates the persisted preference that identifies the specified display.
    /// </summary>
    /// <param name="display">The display to remember.</param>
    /// <returns>The preference carrying the display's identity keys and name.</returns>
    public static HudDisplayPreference ToPreference(DisplayInfo display)
    {

        ArgumentNullException.ThrowIfNull(display);

        return new()
        {
            DevicePath = display.DevicePath,
            EdidKey = display.EdidKey,
            Name = display.Name
        };
    }

    /// <summary>
    /// Finds the display whose work area contains, or is nearest to, the specified point in physical pixels.
    /// </summary>
    /// <param name="displays">The connected displays.</param>
    /// <param name="x">The horizontal coordinate of the point.</param>
    /// <param name="y">The vertical coordinate of the point.</param>
    /// <returns>The nearest display, or <see langword="null"/> when the list is empty.</returns>
    public static DisplayInfo? FindHosting(IReadOnlyList<DisplayInfo> displays, double x, double y)
    {

        ArgumentNullException.ThrowIfNull(displays);

        return displays.MinBy(display => DistanceSquared(display.WorkArea, x, y));
    }

    /// <summary>
    /// Determines whether a preference means "follow the primary display".
    /// </summary>
    /// <param name="preference">The persisted preference.</param>
    /// <returns><see langword="true"/> when no identity key is stored.</returns>
    public static bool IsPrimaryPreference(HudDisplayPreference? preference)
        => preference is null
           || (string.IsNullOrWhiteSpace(preference.DevicePath) && string.IsNullOrWhiteSpace(preference.EdidKey));

    private static DisplayInfo? FindMatch(HudDisplayPreference preference, IReadOnlyList<DisplayInfo> displays)
    {

        if (!string.IsNullOrWhiteSpace(preference.DevicePath))
        {

            var exact = displays.FirstOrDefault(display => string.Equals(display.DevicePath, preference.DevicePath, StringComparison.OrdinalIgnoreCase));

            if (exact is not null)
                return exact;
        }

        if (string.IsNullOrWhiteSpace(preference.EdidKey))
            return null;

        var sameModel = displays
            .Where(display => string.Equals(display.EdidKey, preference.EdidKey, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();

        return sameModel.Count == 1 ? sameModel[0] : null;
    }

    private static double DistanceSquared(ScreenBounds bounds, double x, double y)
    {

        var dx = Math.Max(Math.Max(bounds.Left - x, 0), x - bounds.Right);
        var dy = Math.Max(Math.Max(bounds.Top - y, 0), y - bounds.Bottom);

        return (dx * dx) + (dy * dy);
    }
}
