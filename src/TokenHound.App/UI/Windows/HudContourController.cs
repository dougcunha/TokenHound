using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;
using TokenHound.App.Interop;
using TokenHound.App.UI.Controls;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.UI.Windows;

internal sealed class HudContourController : IDisposable
{

    private readonly Window _owner;
    private readonly HudContourDecorator _decorator;
    private HudShadowWindow? _shadow;
    private DispatcherOperation? _pending;
    private HudShadowInterop.Bounds? _bounds;
    private HudContourGeometry? _contour;
    private Matrix _transform;
    private DpiScale _dpi;
    private bool _failed;
    private bool _disposed;

    internal HudContourController(Window owner, HudContourDecorator decorator)
    {

        _owner = owner;
        _decorator = decorator;
        decorator.FrameChanged += OnChanged;
        owner.LocationChanged += OnChanged;
        owner.SizeChanged += OnSizeChanged;
        owner.DpiChanged += OnDpiChanged;
        owner.IsVisibleChanged += OnVisibleChanged;
        owner.Closed += OnClosed;
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _pending?.Abort();
        _decorator.FrameChanged -= OnChanged;
        _owner.LocationChanged -= OnChanged;
        _owner.SizeChanged -= OnSizeChanged;
        _owner.DpiChanged -= OnDpiChanged;
        _owner.IsVisibleChanged -= OnVisibleChanged;
        _owner.Closed -= OnClosed;
        _shadow?.Close();
        _shadow = null;
        Log.Debug("HUD shadow controller disposed");
    }

    private void OnChanged(object? sender, EventArgs args)
        => Schedule();

    private void OnSizeChanged(object? sender, SizeChangedEventArgs args)
        => Schedule();

    private void OnDpiChanged(object sender, DpiChangedEventArgs args)
        => Schedule();

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        => Schedule();

    private void OnClosed(object? sender, EventArgs args)
        => Dispose();

    private void Schedule()
    {

        if (_disposed || _failed)
            return;

        if (!_owner.IsVisible || _decorator.Contour is null)
            _shadow?.Hide();

        if (_pending?.Status is DispatcherOperationStatus.Pending)
            return;

        _pending = _owner.Dispatcher.InvokeAsync(Synchronize, DispatcherPriority.Render);
    }

    private void Synchronize()
    {

        if (_disposed || _failed || !_owner.IsVisible || _decorator.Contour is not { } contour)
            return;

        if (!_decorator.IsArrangeValid || !_owner.IsArrangeValid)
        {

            _shadow?.Hide();

            return;
        }

        try
        {

            Synchronize(contour);
        }
        catch (Win32Exception exception)
        {

            _failed = true;
            _shadow?.Hide();
            Log.Error(exception, "HUD shadow synchronization failed with Win32 error {NativeError}", exception.NativeErrorCode);
        }
    }

    private void Synchronize(HudContourGeometry contour)
    {

        var bounds = HudShadowInterop.GetBounds(new WindowInteropHelper(_owner).Handle);
        _shadow ??= CreateShadow();
        var handle = new WindowInteropHelper(_shadow).EnsureHandle();

        if (_bounds != bounds)
            HudShadowInterop.SetBounds(handle, bounds);

        var dpi = VisualTreeHelper.GetDpi(_shadow);
        var matrix = ContourTransform(dpi);

        if (_bounds != bounds || _contour != contour || _transform != matrix || !_dpi.Equals(dpi))
            UpdateShadow(
                contour,
                bounds,
                matrix,
                dpi
            );

        if (!_shadow.IsVisible)
            _shadow.Show();
    }

    private HudShadowWindow CreateShadow()
    {

        Log.Debug("Creating passive HUD shadow companion");

        return new HudShadowWindow { Owner = _owner };
    }

    private Matrix ContourTransform(DpiScale shadowDpi)
    {

        var ownerDpi = VisualTreeHelper.GetDpi(_owner);
        var basis = ContourBasis().ForDpi(
            new() { X = ownerDpi.DpiScaleX, Y = ownerDpi.DpiScaleY },
            new() { X = shadowDpi.DpiScaleX, Y = shadowDpi.DpiScaleY }
        );

        return new Matrix(
            basis.Horizontal.X,
            basis.Horizontal.Y,
            basis.Vertical.X,
            basis.Vertical.Y,
            basis.Origin.X,
            basis.Origin.Y
        );
    }

    private HudContourTransform ContourBasis()
    {

        var transform = _decorator.TransformToAncestor(_owner);
        var origin = transform.Transform(new Point());
        var horizontal = transform.Transform(new Point(1, 0)) - origin;
        var vertical = transform.Transform(new Point(0, 1)) - origin;

        return new HudContourTransform
        {
            Origin = new() { X = origin.X, Y = origin.Y },
            Horizontal = new() { X = horizontal.X, Y = horizontal.Y },
            Vertical = new() { X = vertical.X, Y = vertical.Y }
        };
    }

    private void UpdateShadow(
        HudContourGeometry contour,
        HudShadowInterop.Bounds bounds,
        Matrix matrix,
        DpiScale dpi
    )
    {

        _shadow?.SetContour(contour.Envelope, matrix, new Size(bounds.Width / dpi.DpiScaleX, bounds.Height / dpi.DpiScaleY));
        _bounds = bounds;
        _contour = contour;
        _transform = matrix;
        _dpi = dpi;
        Log.Debug(
            "HUD contour synchronized at {Bounds} with DPI {DpiX} in mode {Mode}",
            bounds,
            dpi.DpiScaleX,
            _decorator.Mode
        );
    }
}
