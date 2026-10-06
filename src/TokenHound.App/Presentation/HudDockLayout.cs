using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.Presentation;

/// <summary>
/// Shared, observable docked-edge layout of the HUD capsule. Tooltips render in their own visual trees and the ring
/// panel lives in a template, so both bind to this instance instead of to the window.
/// </summary>
public sealed class HudDockLayout : INotifyPropertyChanged
{
    private const double RING_SPACING = 3;

    /// <summary>
    /// Gets the shared instance used by XAML bindings and the HUD window.
    /// </summary>
    public static HudDockLayout Current { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the screen edge the capsule is docked to.
    /// </summary>
    public HudEdge Edge { get; private set; } = HudEdge.Top;

    /// <summary>
    /// Gets a value indicating whether the capsule stacks providers vertically.
    /// </summary>
    public bool IsVertical { get; private set; }

    /// <summary>
    /// Gets the stacking direction of the provider rings.
    /// </summary>
    public Orientation Orientation
        => IsVertical ? Orientation.Vertical : Orientation.Horizontal;

    /// <summary>
    /// Gets the spacing around each provider ring along the stacking direction.
    /// </summary>
    public Thickness RingMargin
        => IsVertical
            ? new Thickness(RING_SPACING) { Left = 0, Right = 0 }
            : new Thickness(RING_SPACING) { Top = 0, Bottom = 0 };

    /// <summary>
    /// Gets the provider tooltip placement, opening toward the inside of the screen on side edges.
    /// </summary>
    public PlacementMode TooltipPlacement
        => Edge switch
        {
            HudEdge.Left => PlacementMode.Right,
            HudEdge.Right => PlacementMode.Left,
            _ => PlacementMode.Mouse
        };

    /// <summary>
    /// Applies the docked edge and orientation, notifying bindings only when something changed.
    /// </summary>
    /// <param name="edge">The docked edge.</param>
    /// <param name="isVertical">Whether the capsule is vertical.</param>
    public void Apply(HudEdge edge, bool isVertical)
    {

        if (edge == Edge && isVertical == IsVertical)
            return;

        Edge = edge;
        IsVertical = isVertical;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
