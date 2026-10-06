using Serilog;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TokenHound.App.Interop;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private static readonly TimeSpan PLACEMENT_COALESCE_DELAY = TimeSpan.FromMilliseconds(300);

    private bool _applyingPlacement;
    private bool _modeWarningLogged;
    private HudPlacementService? _placement;
    private DispatcherTimer? _placementTimer;

    /// <summary>
    /// Gets or sets the placement service that owns the HUD mode, preferred display, and Free position.
    /// </summary>
    public HudPlacementService? Placement
    {
        get => _placement;
        set
        {

            if (ReferenceEquals(_placement, value))
                return;

            if (_placement is not null)
                _placement.Changed -= OnPlacementChanged;

            _placement = value;

            if (_placement is not null)
                _placement.Changed += OnPlacementChanged;
        }
    }

    /// <summary>
    /// Gets or sets the catalog listing the connected displays for docked placement.
    /// </summary>
    public DisplayCatalog? Displays { get; set; }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {

        ApplyChrome();
        ApplyPlacement();
    }

    // Display and work-area messages arrive in bursts; one pass after the burst settles is enough.
    private void SchedulePlacement()
    {

        _placementTimer ??= new DispatcherTimer(
            PLACEMENT_COALESCE_DELAY,
            DispatcherPriority.Background,
            OnPlacementTimerTick,
            Dispatcher
        );

        _placementTimer.Stop();
        _placementTimer.Start();
    }

    private void OnPlacementTimerTick(object? sender, EventArgs e)
    {

        _placementTimer?.Stop();
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {

        if (_applyingPlacement || _placement is null)
            return;

        _applyingPlacement = true;

        try
        {

            var (mode, warning) = _placement.Current.ResolveMode();
            LogModeWarning(warning);

            if (mode == HudDockMode.Free)
                ApplyFreePlacement(_placement);
            else
                ApplyDockedPlacement(_placement, mode);
        }
        finally
        {

            _applyingPlacement = false;
        }
    }

    private void ApplyFreePlacement(HudPlacementService placement)
    {

        if (!placement.Current.TryGetPosition(out var left, out var top))
            return;

        ApplyMonitorPlacement(left, top);
        placement.UpdateFreePosition(Left, Top);
    }

    private void ApplyDockedPlacement(HudPlacementService placement, HudDockMode mode)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        var window = WindowPlacement.GetWindowPixelBounds(hwnd);
        var displays = Displays?.GetDisplays() ?? [];

        if (window is null || displays.Count == 0)
            return;

        // During SizeChanged the native rectangle still has the previous size; the laid-out size is already current.
        var dpi = VisualTreeHelper.GetDpi(this);
        var target = DisplayResolver.Resolve(placement.Current.Display, displays).Target;
        var (left, top) = NotchPlacement.Dock(
            mode,
            target.WorkArea,
            Math.Round(ActualWidth * dpi.DpiScaleX),
            Math.Round(ActualHeight * dpi.DpiScaleY)
        );

        if ((int)left == (int)window.Left && (int)top == (int)window.Top)
            return;

        WindowPlacement.MoveWindowTo(hwnd, (int)left, (int)top);
        Log.Debug(
            "HUD placement {Mode} on {Display} at {X},{Y}",
            mode,
            target.Name,
            left,
            top
        );
    }

    private void ApplyMonitorPlacement(double left, double top)
    {

        // Resolve the nearest real monitor after WPF applies the position and any DPI transition.
        (Left, Top) = (left, top);
        var hwnd = new WindowInteropHelper(this).Handle;
        var workArea = WindowPlacement.GetWorkArea(hwnd, VisualTreeHelper.GetDpi(this));

        (Left, Top) = NotchPlacement.Clamp(
            ToBounds(workArea),
            Left,
            Top,
            ActualWidth,
            ActualHeight
        );
    }

    private void LogModeWarning(string? warning)
    {

        if (warning is null || _modeWarningLogged)
            return;

        _modeWarningLogged = true;
        Log.Warning("{PlacementWarning}", warning);
    }

    /// <summary>
    /// Captures the connected displays, the display hosting the HUD, and the HUD position for a placement choice.
    /// </summary>
    /// <returns>The placement context.</returns>
    public HudPlacementContext CreatePlacementContext()
    {

        var displays = Displays?.GetDisplays() ?? [];
        var window = WindowPlacement.GetWindowPixelBounds(new WindowInteropHelper(this).Handle);
        var hosting = window is null
            ? null
            : DisplayResolver.FindHosting(displays, window.Left + (window.Width / 2.0), window.Top + (window.Height / 2.0));

        return new HudPlacementContext
        {
            Displays = displays,
            Hosting = hosting,
            Left = Left,
            Top = Top
        };
    }

    private static ScreenBounds ToBounds(Rect bounds)
        => new()
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height
        };
}
