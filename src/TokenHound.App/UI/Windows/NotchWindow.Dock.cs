using System.Windows;
using System.Windows.Controls.Primitives;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private const double GUTTER = 12;
    private const double STATUS_POPUP_GAP = 4;

    private static readonly Thickness HORIZONTAL_PADDING = new(8) { Top = 5, Bottom = 5 };
    private static readonly Thickness VERTICAL_PADDING = new(8) { Left = 5, Right = 5 };

    // Applies the edge-specific capsule outline before placement so docking measures the final orientation.
    private void ApplyChrome()
    {

        var mode = _placement?.Mode ?? HudDockMode.TopCenter;
        var (edge, isVertical) = HudEdgeLayout.For(mode);

        HudDockLayout.Current.Apply(edge, isVertical);
        RootGrid.Margin = GutterFor(mode);
        CapsuleBorder.Mode = mode;
        CapsuleBorder.Padding = isVertical ? VERTICAL_PADDING : HORIZONTAL_PADDING;
        ApplyStatusPopupPlacement(edge);
        ApplyScaledMinimums();
    }

    private void ApplyStatusPopupPlacement(HudEdge edge)
    {

        (StatusPopup.Placement, StatusPopup.HorizontalOffset, StatusPopup.VerticalOffset) = edge switch
        {
            HudEdge.Left => (PlacementMode.Right, STATUS_POPUP_GAP, 0.0),
            HudEdge.Right => (PlacementMode.Left, -STATUS_POPUP_GAP, 0.0),
            _ => (PlacementMode.Bottom, 0.0, STATUS_POPUP_GAP)
        };
    }

    // The gutter hosts the drop shadow; it is zero on every side that touches the docked screen edge.
    private static Thickness GutterFor(HudDockMode mode)
        => mode switch
        {
            HudDockMode.TopLeft => new Thickness(GUTTER) { Left = 0, Top = 0 },
            HudDockMode.TopRight => new Thickness(GUTTER) { Top = 0, Right = 0 },
            HudDockMode.LeftEdge => new Thickness(GUTTER) { Left = 0 },
            HudDockMode.RightEdge => new Thickness(GUTTER) { Right = 0 },
            _ => new Thickness(GUTTER) { Top = 0 }
        };
}
