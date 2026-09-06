using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TokenHound.Core.Models;

namespace TokenHound.App.UI.Controls;

/// <summary>
/// Renders a circular quota consumption ring with dynamic status colors, badge text, and activity pulsing.
/// </summary>
public sealed partial class ProviderRing : UserControl
{
    private const double CENTER_X = 22.0;
    private const double CENTER_Y = 22.0;
    private const double RADIUS = 18.0;

    private static readonly SolidColorBrush GREEN_BRUSH = CreateFrozenBrush(0x10, 0xB9, 0x81);
    private static readonly SolidColorBrush AMBER_BRUSH = CreateFrozenBrush(0xF5, 0x9E, 0x0B);
    private static readonly SolidColorBrush RED_BRUSH = CreateFrozenBrush(0xEF, 0x44, 0x44);
    private static readonly SolidColorBrush PURPLE_BRUSH = CreateFrozenBrush(0x8B, 0x5C, 0xF6);
    private static readonly SolidColorBrush GREY_BRUSH = CreateFrozenBrush(0x6B, 0x72, 0x80);
    private static readonly SolidColorBrush TRACK_BRUSH = CreateFrozenBrush(0x27, 0x27, 0x2A);
    private static readonly DoubleCollection UNMEASURED_DASH = CreateFrozenDash();

    private readonly Storyboard? _pulseStoryboard;

    /// <summary>Identifies the <see cref="UsedFraction"/> dependency property.</summary>
    public static readonly DependencyProperty UsedFractionProperty =
        DependencyProperty.Register(
            nameof(UsedFraction),
            typeof(double?),
            typeof(ProviderRing),
            new PropertyMetadata(null, OnVisualPropertyChanged)
        );

    /// <summary>Identifies the <see cref="ProviderBadge"/> dependency property.</summary>
    public static readonly DependencyProperty ProviderBadgeProperty =
        DependencyProperty.Register(
            nameof(ProviderBadge),
            typeof(string),
            typeof(ProviderRing),
            new PropertyMetadata(string.Empty, OnBadgePropertyChanged)
        );

    /// <summary>Identifies the <see cref="Status"/> dependency property.</summary>
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(ProviderStatus),
            typeof(ProviderRing),
            new PropertyMetadata(ProviderStatus.Ok, OnVisualPropertyChanged)
        );

    /// <summary>Identifies the <see cref="IsBusy"/> dependency property.</summary>
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(
            nameof(IsBusy),
            typeof(bool),
            typeof(ProviderRing),
            new PropertyMetadata(false, OnBusyPropertyChanged)
        );

    /// <summary>Initializes a new instance of the <see cref="ProviderRing"/> class.</summary>
    public ProviderRing()
    {

        InitializeComponent();
        _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;
        UpdateVisuals();
    }

    /// <summary>Gets or sets the quota utilization fraction (0.0 to 1.0), or null if unmeasured.</summary>
    public double? UsedFraction
    {
        get
            => (double?)GetValue(UsedFractionProperty);
        set
            => SetValue(UsedFractionProperty, value);
    }

    /// <summary>Gets or sets the short badge glyph displayed in the center of the ring.</summary>
    public string ProviderBadge
    {
        get
            => (string)GetValue(ProviderBadgeProperty);
        set
            => SetValue(ProviderBadgeProperty, value);
    }

    /// <summary>Gets or sets the operational and health status of the provider.</summary>
    public ProviderStatus Status
    {
        get
            => (ProviderStatus)GetValue(StatusProperty);
        set
            => SetValue(StatusProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether a session process is actively executing tasks.</summary>
    public bool IsBusy
    {
        get
            => (bool)GetValue(IsBusyProperty);
        set
            => SetValue(IsBusyProperty, value);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

        if (d is ProviderRing ring)
            ring.UpdateVisuals();
    }

    private static void OnBadgePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

        if (d is ProviderRing ring)
        {
            ring.BadgeTextBlock.Text = ring.ProviderBadge;
            ring.UpdateBusyIndicator();
        }
    }

    private static void OnBusyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

        if (d is ProviderRing ring)
            ring.UpdateBusyIndicator();
    }

    private void UpdateVisuals()
    {

        var brush = ResolveStatusBrush(Status, UsedFraction);
        BusyIndicator.Fill = brush;
        Opacity = Status == ProviderStatus.Stale ? 0.5 : 1.0;

        if (UsedFraction.HasValue && UsedFraction.Value > 0.0)
        {
            ArcProgressPath.Data = CreateArcGeometry(UsedFraction.Value);
            ArcProgressPath.Stroke = brush;
            ArcProgressPath.Visibility = Visibility.Visible;
            StatusDot.Visibility = Visibility.Collapsed;
            TrackRing.Stroke = TRACK_BRUSH;
            TrackRing.StrokeDashArray = null;
        }
        else
        {
            ArcProgressPath.Data = null;
            ArcProgressPath.Visibility = Visibility.Collapsed;
            StatusDot.Visibility = Visibility.Visible;
            StatusDot.Fill = brush;
            TrackRing.Stroke = brush;
            TrackRing.StrokeDashArray = UNMEASURED_DASH;
        }

        UpdateBusyIndicator();
    }

    private void UpdateBusyIndicator()
    {

        if (!IsBusy)
        {
            BusyIndicator.Visibility = Visibility.Collapsed;
            _pulseStoryboard?.Stop(this);
            return;
        }

        BusyIndicator.Visibility = Visibility.Visible;

        if (string.IsNullOrWhiteSpace(ProviderBadge))
        {
            BusyIndicator.VerticalAlignment = VerticalAlignment.Center;
            BusyIndicator.Margin = new Thickness(0);
        }
        else
        {
            BusyIndicator.VerticalAlignment = VerticalAlignment.Bottom;
            BusyIndicator.Margin = new Thickness(0, 0, 0, 7);
        }

        _pulseStoryboard?.Begin(this, true);
    }

    private static Geometry? CreateArcGeometry(double fraction)
    {

        if (fraction <= 0.0001)
            return null;

        var clamped = Math.Min(fraction, 1.0);
        var angle = clamped >= 0.9999 ? 359.99 : clamped * 360.0;
        var radians = angle * Math.PI / 180.0;
        var endX = CENTER_X + RADIUS * Math.Sin(radians);
        var endY = CENTER_Y - RADIUS * Math.Cos(radians);

        var segment = new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(RADIUS, RADIUS),
            RotationAngle = 0.0,
            IsLargeArc = angle > 180.0,
            SweepDirection = SweepDirection.Clockwise,
            IsStroked = true
        };

        var figure = new PathFigure
        {
            StartPoint = new Point(CENTER_X, CENTER_Y - RADIUS),
            Segments = [segment],
            IsClosed = false
        };

        var geometry = new PathGeometry([figure]);
        geometry.Freeze();

        return geometry;
    }

    private static SolidColorBrush ResolveStatusBrush(
        ProviderStatus status,
        double? fraction)
    {

        if (status == ProviderStatus.NeedsAuth)
            return PURPLE_BRUSH;

        if (status == ProviderStatus.Stale)
            return GREY_BRUSH;

        if (status is ProviderStatus.RateLimited or ProviderStatus.AccessDenied)
            return RED_BRUSH;

        if (fraction.HasValue)
        {

            if (fraction.Value >= 0.9)
                return RED_BRUSH;

            if (fraction.Value >= 0.7)
                return AMBER_BRUSH;

            return GREEN_BRUSH;
        }

        return GREEN_BRUSH;
    }

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {

        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();

        return brush;
    }

    private static DoubleCollection CreateFrozenDash()
    {

        var collection = new DoubleCollection([3, 2]);
        collection.Freeze();

        return collection;
    }
}
