using Serilog;
using Serilog.Events;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using TokenHound.App.Interop;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Controls;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Chooses between the Acrylic material and the solid fill, drives the backdrop companion from the contour frame,
/// and swaps the capsule fill in the same pass. Any native failure falls back to the solid fill for the session.
/// </summary>
internal sealed class HudBackdropController : IDisposable
{

    /// <summary>Hide reason for a frame whose arrange is invalid; its transitions are transient and log at Debug.</summary>
    internal const string ARRANGE_INVALID_REASON = "ArrangeInvalid";
    private const string FAILURE_REASON = "Failure";
    private const string OPAQUE_REASON = "Opaque";
    private readonly HudContourDecorator _decorator;
    private readonly HudBackdropPreference _preference = HudBackdropPreference.Current;
    private readonly Brush _solid;
    private readonly HudBackdropAvailability _availability;
    private readonly HudBackdropWindow _window = new();
    private SolidColorBrush _tint;
    private HudBackdropMode? _mode;
    private bool _failed;
    private bool _transient;

    internal HudBackdropController(Window owner, HudContourDecorator decorator)
    {

        _decorator = decorator;
        _solid = decorator.Background ?? CreateFill(byte.MaxValue);
        _tint = CreateTint();
        _availability = new HudBackdropAvailability(owner, _preference);
        _availability.Changed += OnAvailabilityChanged;
        _preference.PropertyChanged += OnPreferenceChanged;
    }

    /// <summary>Raised when the material availability changed and the frame must be synchronized again.</summary>
    internal event EventHandler? Changed;

    /// <summary>Applies the current mode to the companion and the capsule fill for one contour frame.</summary>
    internal void Synchronize(IntPtr hud, Geometry contour, Matrix pixelTransform, HudShadowInterop.Bounds bounds)
    {

        var reason = ResolveReason(_availability.Inputs);
        var mode = string.Equals(reason, HudBackdropPolicy.AVAILABLE, StringComparison.OrdinalIgnoreCase)
            ? HudBackdropMode.Material
            : HudBackdropMode.Solid;

        if (mode is HudBackdropMode.Material
            && !TryShow(
                hud,
                contour,
                pixelTransform,
                bounds
            ))
            mode = HudBackdropMode.Solid;

        if (mode is HudBackdropMode.Solid)
            _window.Hide();

        Apply(mode, _failed ? FAILURE_REASON : reason);
    }

    /// <summary>
    /// Hides the companion while the HUD is hidden, its frame is invalid, or synchronization stopped,
    /// and shows the solid fill so no frame keeps the tint without the material.
    /// </summary>
    internal void Hide(string reason)
    {

        _window.Hide();
        Apply(HudBackdropMode.Solid, reason);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        _preference.PropertyChanged -= OnPreferenceChanged;
        _availability.Changed -= OnAvailabilityChanged;
        _availability.Dispose();
        _window.Dispose();
    }

    private bool TryShow(IntPtr hud, Geometry contour, Matrix pixelTransform, HudShadowInterop.Bounds bounds)
    {

        try
        {

            _window.Synchronize(
                hud,
                contour,
                pixelTransform,
                bounds
            );

            return true;
        }
        catch (Exception exception)
        {

            _failed = true;
            _window.Dispose();
            Log.Warning(exception, "HUD backdrop unavailable; using the solid fill ({HResult})", exception.HResult);

            return false;
        }
    }

    private void Apply(HudBackdropMode mode, string reason)
    {

        var transient = string.Equals(reason, ARRANGE_INVALID_REASON, StringComparison.OrdinalIgnoreCase);
        var afterTransient = _transient;
        _transient = transient;

        if (_mode == mode)
            return;

        var level = transient || afterTransient ? LogEventLevel.Debug : LogEventLevel.Information;
        _mode = mode;
        _decorator.Background = mode is HudBackdropMode.Material ? _tint : _solid;
        Log.Write(level, "HUD background switched to {Mode} ({Reason})", mode, reason);
    }

    private void OnAvailabilityChanged(object? sender, EventArgs args)
        => Changed?.Invoke(this, EventArgs.Empty);

    private void OnPreferenceChanged(object? sender, PropertyChangedEventArgs args)
    {

        if (!string.IsNullOrEmpty(args.PropertyName)
            && !string.Equals(args.PropertyName, nameof(HudBackdropPreference.Transparency), StringComparison.OrdinalIgnoreCase))
            return;

        _tint = CreateTint();

        if (_mode is HudBackdropMode.Material)
            _decorator.Background = _tint;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Names the input that keeps the solid fill; a fully opaque tint would hide the material, so it is skipped.</summary>
    private string ResolveReason(HudBackdropInputs inputs)
    {

        if (_failed)
            return FAILURE_REASON;

        var reason = HudBackdropPolicy.Reason(inputs);

        return string.Equals(reason, HudBackdropPolicy.AVAILABLE, StringComparison.OrdinalIgnoreCase)
            && _preference.Transparency <= HudBackdropSettings.MINIMUM_TRANSPARENCY
                ? OPAQUE_REASON
                : reason;
    }

    private SolidColorBrush CreateTint()
        => CreateFill(HudBackdropPreference.TintAlpha(_preference.Transparency));

    private static SolidColorBrush CreateFill(byte alpha)
    {

        var brush = new SolidColorBrush(
            Color.FromArgb(
                alpha,
                0x18,
                0x18,
                0x1B
            )
        );
        brush.Freeze();

        return brush;
    }
}
