using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Controls;

/// <summary>Paints edge-aware HUD chrome around its existing provider content.</summary>
public sealed class HudContourDecorator : Decorator
{

    /// <summary>Identifies the provider padding property.</summary>
    public static readonly DependencyProperty PADDING_PROPERTY = DependencyProperty.Register(
        nameof(Padding),
        typeof(Thickness),
        typeof(HudContourDecorator),
        new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
    );

    /// <summary>Identifies the docking mode property.</summary>
    public static readonly DependencyProperty MODE_PROPERTY = DependencyProperty.Register(
        nameof(Mode),
        typeof(HudDockMode),
        typeof(HudContourDecorator),
        new FrameworkPropertyMetadata(HudDockMode.TopCenter, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
    );

    /// <summary>Identifies the capsule fill property.</summary>
    public static readonly DependencyProperty BACKGROUND_PROPERTY = Panel.BackgroundProperty.AddOwner(
        typeof(HudContourDecorator),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender)
    );

    /// <summary>Identifies the exposed stroke brush property.</summary>
    public static readonly DependencyProperty BORDER_BRUSH_PROPERTY = Border.BorderBrushProperty.AddOwner(typeof(HudContourDecorator));

    private const double STROKE_THICKNESS = 1.5;
    private Size _body;
    private HudContourLayout.Request? _request;

    /// <summary>Gets or sets the padding within the contour's safe provider body.</summary>
    public Thickness Padding
    {
        get => (Thickness)GetValue(PADDING_PROPERTY);
        set => SetValue(PADDING_PROPERTY, value);
    }

    /// <summary>Gets or sets the current contour placement.</summary>
    public HudDockMode Mode
    {
        get => (HudDockMode)GetValue(MODE_PROPERTY);
        set => SetValue(MODE_PROPERTY, value);
    }

    /// <summary>Gets or sets the painted fill.</summary>
    public Brush Background
    {
        get => (Brush)GetValue(BACKGROUND_PROPERTY);
        set => SetValue(BACKGROUND_PROPERTY, value);
    }

    /// <summary>Gets or sets the painted stroke brush.</summary>
    public Brush BorderBrush
    {
        get => (Brush)GetValue(BORDER_BRUSH_PROPERTY);
        set => SetValue(BORDER_BRUSH_PROPERTY, value);
    }

    internal event EventHandler? FrameChanged;

    internal HudContourGeometry? Contour { get; private set; }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {

        Child?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var child = Child?.DesiredSize ?? default;
        _body = new Size(child.Width + Padding.Left + Padding.Right, child.Height + Padding.Top + Padding.Bottom);
        var extent = HudContourLayout.Measure(Request(default));

        return new Size(extent.X, extent.Y);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size arrangeSize)
    {

        var request = Request(arrangeSize);

        if (request != _request)
            UpdateContour(request);

        var frame = HudContourLayout.Create(request);

        if (frame is not null)
            Child?.Arrange(new Rect(
                frame.BodyOrigin.X + Padding.Left,
                frame.BodyOrigin.Y + Padding.Top,
                Math.Max(0, frame.BodyWidth - Padding.Left - Padding.Right),
                Math.Max(0, frame.BodyHeight - Padding.Top - Padding.Bottom)
            ));

        return arrangeSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {

        if (Contour is null)
            return;

        drawingContext.DrawGeometry(Background, null, Contour.Fill);
        var pen = new Pen(BorderBrush, STROKE_THICKNESS);
        pen.Freeze();
        drawingContext.DrawGeometry(null, pen, Contour.Stroke);
    }

    /// <inheritdoc />
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        => Contour?.Envelope.FillContains(hitTestParameters.HitPoint) == true
            ? new PointHitTestResult(this, hitTestParameters.HitPoint) : null;

    private HudContourLayout.Request Request(Size size)
        => new() { Mode = Mode, BodyWidth = _body.Width, BodyHeight = _body.Height, Width = size.Width, Height = size.Height };

    private void UpdateContour(HudContourLayout.Request request)
    {

        _request = request;
        var frame = HudContourLayout.Create(request);
        Contour = frame is null ? null : new HudContourGeometry(frame);
        Clip = Contour?.Envelope;
        InvalidateVisual();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void OnLayoutPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {

        var decorator = (HudContourDecorator)sender;
        decorator._request = null;
        decorator.Contour = null;
        decorator.FrameChanged?.Invoke(decorator, EventArgs.Empty);
    }
}
