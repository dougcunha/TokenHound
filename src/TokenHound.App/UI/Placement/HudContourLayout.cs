using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Placement;

/// <summary>Builds all HUD contours from one horizontal, top-attached definition.</summary>
internal static class HudContourLayout
{

    private const double CONVEX_RADIUS = 24;
    private const double JOIN_RADIUS = 12;
    private const double CIRCLE_CONTROL = 0.5522847498307936;

    /// <summary>Required body and arranged host extents, including provider padding but no outer gutter.</summary>
    internal sealed record Request
    {

        /// <summary>Gets the existing docking mode.</summary>
        public required HudDockMode Mode { get; init; }

        /// <summary>Gets the minimum required provider-body width.</summary>
        public required double BodyWidth { get; init; }

        /// <summary>Gets the minimum required provider-body height.</summary>
        public required double BodyHeight { get; init; }

        /// <summary>Gets the available arranged width.</summary>
        public required double Width { get; init; }

        /// <summary>Gets the available arranged height.</summary>
        public required double Height { get; init; }

        /// <summary>Gets the existing chrome stroke thickness.</summary>
        public double StrokeThickness { get; init; } = 1.5;
    }

    /// <summary>Gets the desired host extents without shrinking the provider body.</summary>
    public static HudContourPoint Measure(Request request)
    {

        Validate(request);
        var vertical = HudEdgeLayout.For(request.Mode).IsVertical;
        var width = vertical ? request.BodyHeight : request.BodyWidth;
        var height = vertical ? request.BodyWidth : request.BodyHeight;
        var join = Math.Min(JOIN_RADIUS, Math.Min(width, height) / 2);
        var wings = request.Mode is HudDockMode.Free ? 0 : request.Mode is HudDockMode.TopLeft or HudDockMode.TopRight ? 1 : 2;
        var stroke = request.StrokeThickness;
        var sideStroke = request.Mode is HudDockMode.TopLeft or HudDockMode.TopRight ? stroke / 2 : stroke;
        var size = Point(width + wings * join + sideStroke, height + (request.Mode is HudDockMode.Free ? stroke : stroke / 2));
        RequireExtent(size.X, nameof(request.Width));
        RequireExtent(size.Y, nameof(request.Height));

        return vertical ? Point(size.Y, size.X) : size;
    }

    /// <summary>Creates an arranged contour; zero-sized pre-layout hosts have no drawable frame.</summary>
    public static HudContourFrame? Create(Request request)
    {

        Validate(request);

        if (request.Width == 0 || request.Height == 0)
            return null;

        var vertical = HudEdgeLayout.For(request.Mode).IsVertical;
        var canonical = vertical ? request with
        {

            Mode = HudDockMode.TopCenter,
            BodyWidth = request.BodyHeight,
            BodyHeight = request.BodyWidth,
            Width = request.Height,
            Height = request.Width
        } : request;
        var frame = Build(canonical);

        return vertical ? Transform(frame, request) : frame;
    }

    private static void Validate(Request request)
    {

        ArgumentNullException.ThrowIfNull(request);
        _ = HudEdgeLayout.For(request.Mode);
        RequireExtent(request.BodyWidth, nameof(request.BodyWidth));
        RequireExtent(request.BodyHeight, nameof(request.BodyHeight));
        RequireExtent(request.Width, nameof(request.Width));
        RequireExtent(request.Height, nameof(request.Height));
        RequireExtent(request.StrokeThickness, nameof(request.StrokeThickness));
    }

    private static void RequireExtent(double value, string name)
    {

        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, value, "Contour extents must be finite and nonnegative.");
    }

    private sealed record Shape
    {

        /// <summary>Gets whether all four corners are convex.</summary>
        public required bool Free { get; init; }

        /// <summary>Gets the canonical width inside the stroke inset.</summary>
        public required double Width { get; init; }

        /// <summary>Gets the canonical height inside the stroke inset.</summary>
        public required double Height { get; init; }

        /// <summary>Gets the left provider-body edge.</summary>
        public required double Left { get; init; }

        /// <summary>Gets the right provider-body edge.</summary>
        public required double Right { get; init; }

        /// <summary>Gets the bounded convex radius.</summary>
        public required double Radius { get; init; }

        /// <summary>Gets the canonical stroke offset.</summary>
        public required HudContourPoint Origin { get; init; }
    }

    private static HudContourFrame Build(Request request)
    {

        var shape = CreateShape(request);

        return Frame(request, shape, BuildSegments(request.Mode, shape));
    }

    private static Shape CreateShape(Request request)
    {

        var free = request.Mode is HudDockMode.Free;
        var half = request.StrokeThickness / 2;
        var inset = request.Mode is HudDockMode.TopLeft ? 0 : half;
        var width = request.Width - inset - (request.Mode is HudDockMode.TopRight ? 0 : half);
        var height = request.Height - (free ? request.StrokeThickness : half);

        if (width < request.BodyWidth || height < request.BodyHeight)
            throw new ArgumentException("The arranged host cannot contain the required provider body.", nameof(request));

        return CompleteShape(request, Point(width, height), Point(inset, free ? half : 0));
    }

    private static Shape CompleteShape(Request request, HudContourPoint size, HudContourPoint origin)
    {

        var free = request.Mode is HudDockMode.Free;
        var width = size.X;
        var height = size.Y;
        var wings = free ? 0 : request.Mode is HudDockMode.TopLeft or HudDockMode.TopRight ? 1 : 2;
        var join = wings == 0 ? 0 : Math.Min(JOIN_RADIUS, Math.Min((width - request.BodyWidth) / wings, height / 2));
        join = Math.Min(join, request.BodyWidth / 2);
        var left = request.Mode is HudDockMode.TopLeft or HudDockMode.Free ? 0 : join;
        var right = width - (request.Mode is HudDockMode.TopRight or HudDockMode.Free ? 0 : join);
        var radius = Math.Min(CONVEX_RADIUS, Math.Min(right - left, height) / 2);
        return new Shape
        {

            Free = free, Width = width, Height = height, Left = left, Right = right,
            Radius = radius, Origin = origin
        };
    }

    private static List<HudContourSegment> BuildSegments(HudDockMode mode, Shape shape)
    {

        List<HudContourSegment> segments = [];
        Line(segments, Point(shape.Free ? shape.Right - shape.Radius : shape.Width, 0), shape.Free);
        TopRight(segments, shape);
        Line(segments, Point(shape.Right, shape.Height - shape.Radius), mode is not HudDockMode.TopRight);
        Bottom(segments, shape);
        Line(segments, Point(shape.Left, shape.Free ? shape.Radius : shape.Left), mode is not HudDockMode.TopLeft);
        TopLeft(segments, shape);

        return segments;
    }

    private static void Bottom(List<HudContourSegment> segments, Shape shape)
    {

        var right = shape.Right;
        var left = shape.Left;
        var height = shape.Height;
        var radius = shape.Radius;
        Curve(
            segments,
            Point(right, height - radius + radius * CIRCLE_CONTROL),
            Point(right - radius + radius * CIRCLE_CONTROL, height),
            Point(right - radius, height)
        );
        Line(segments, Point(left + radius, height), true);
        Curve(
            segments,
            Point(left + radius - radius * CIRCLE_CONTROL, height),
            Point(left, height - radius + radius * CIRCLE_CONTROL),
            Point(left, height - radius)
        );
    }

    private static void TopRight(List<HudContourSegment> segments, Shape shape)
    {

        var join = shape.Free ? shape.Radius : shape.Width - shape.Right;
        var control1 = shape.Free
            ? Point(shape.Right - shape.Radius + shape.Radius * CIRCLE_CONTROL, 0)
            : Point(shape.Width - join * CIRCLE_CONTROL, 0);
        var control2 = Point(shape.Right, join - join * CIRCLE_CONTROL);
        Curve(
            segments,
            control1,
            control2,
            Point(shape.Right, join)
        );
    }

    private static void TopLeft(List<HudContourSegment> segments, Shape shape)
    {

        var join = shape.Free ? shape.Radius : shape.Left;
        var control1 = Point(shape.Left, join - join * CIRCLE_CONTROL);
        var control2 = shape.Free
            ? Point(shape.Left + shape.Radius - shape.Radius * CIRCLE_CONTROL, 0)
            : Point(join * CIRCLE_CONTROL, 0);
        Curve(
            segments,
            control1,
            control2,
            Point(shape.Free ? shape.Left + shape.Radius : 0, 0)
        );
    }

    private static HudContourFrame Frame(Request request, Shape shape, List<HudContourSegment> segments)
    {

        HudContourPoint Offset(HudContourPoint point)
            => Point(point.X + shape.Origin.X, point.Y + shape.Origin.Y);
        var mapped = segments.Select(segment => Map(segment, Offset)).ToArray();

        return new HudContourFrame
        {

            Mode = request.Mode, Width = request.Width, Height = request.Height,
            BodyOrigin = Offset(Point(shape.Left, 0)), BodyWidth = shape.Right - shape.Left, BodyHeight = shape.Height,
            Start = mapped[^1].End, Segments = Array.AsReadOnly(mapped), StrokeThickness = request.StrokeThickness
        };
    }

    private static HudContourFrame Transform(HudContourFrame frame, Request request)
    {

        HudContourPoint Convert(HudContourPoint point)
            => request.Mode is HudDockMode.LeftEdge
                ? Point(point.Y, point.X) : Point(request.Width - point.Y, point.X);
        var body = Convert(frame.BodyOrigin);

        return frame with
        {

            Mode = request.Mode, Width = request.Width, Height = request.Height,
            BodyOrigin = request.Mode is HudDockMode.LeftEdge ? body : Point(body.X - frame.BodyHeight, body.Y),
            BodyWidth = frame.BodyHeight, BodyHeight = frame.BodyWidth,
            Start = Convert(frame.Start), Segments = Array.AsReadOnly(frame.Segments.Select(segment => Map(segment, Convert)).ToArray())
        };
    }

    private static HudContourSegment Map(HudContourSegment segment, Func<HudContourPoint, HudContourPoint> map)
        => segment with
        {

            End = map(segment.End),
            Control1 = segment.Control1 is null ? null : map(segment.Control1),
            Control2 = segment.Control2 is null ? null : map(segment.Control2)
        };

    private static HudContourPoint Point(double x, double y)
        => new() { X = x, Y = y };

    private static void Line(List<HudContourSegment> segments, HudContourPoint end, bool stroked)
        => segments.Add(new HudContourSegment { End = end, IsStroked = stroked });

    private static void Curve(
        List<HudContourSegment> segments,
        HudContourPoint control1,
        HudContourPoint control2,
        HudContourPoint end
    )
        => segments.Add(new HudContourSegment { End = end, Control1 = control1, Control2 = control2, IsStroked = true });
}
