namespace TokenHound.App.UI.Placement;

/// <summary>Maps a contour's local basis into a window's device-independent coordinates.</summary>
internal sealed record HudContourTransform
{

    /// <summary>Gets the transformed local origin.</summary>
    public required HudContourPoint Origin { get; init; }

    /// <summary>Gets the transformed horizontal unit vector.</summary>
    public required HudContourPoint Horizontal { get; init; }

    /// <summary>Gets the transformed vertical unit vector.</summary>
    public required HudContourPoint Vertical { get; init; }

    /// <summary>Converts a SizeToContent extent to WPF's whole physical-pixel coverage.</summary>
    public static double PixelExtent(double extent, double dpi)
    {

        if (!double.IsFinite(extent) || extent < 0)
            throw new ArgumentOutOfRangeException(nameof(extent));

        if (!double.IsFinite(dpi) || dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi));

        return Math.Ceiling(extent * dpi);
    }

    /// <summary>Converts the complete basis between window DPI scales, preserving physical alignment.</summary>
    public HudContourTransform ForDpi(HudContourPoint ownerDpi, HudContourPoint shadowDpi)
    {

        ValidateDpi(ownerDpi);
        ValidateDpi(shadowDpi);
        var ratioX = ownerDpi.X / shadowDpi.X;
        var ratioY = ownerDpi.Y / shadowDpi.Y;

        return new HudContourTransform
        {
            Origin = Scale(Origin, ratioX, ratioY),
            Horizontal = Scale(Horizontal, ratioX, ratioY),
            Vertical = Scale(Vertical, ratioX, ratioY)
        };
    }

    private static HudContourPoint Scale(HudContourPoint point, double scaleX, double scaleY)
        => new() { X = point.X * scaleX, Y = point.Y * scaleY };

    private static void ValidateDpi(HudContourPoint dpi)
    {

        if (!double.IsFinite(dpi.X) || !double.IsFinite(dpi.Y) || dpi.X <= 0 || dpi.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI scales must be finite and positive.");
    }
}
