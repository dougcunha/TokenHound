using System.Windows;
using System.Windows.Media;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.UI.Controls;

internal sealed class HudContourGeometry
{

    internal HudContourGeometry(HudContourFrame frame)
    {

        Fill = Build(frame, false);
        Stroke = Build(frame, true);
        StrokePen = new Pen(Brushes.Black, frame.StrokeThickness);
        StrokePen.Freeze();
        Envelope = Geometry.Combine(
            Fill,
            Stroke.GetWidenedPathGeometry(StrokePen),
            GeometryCombineMode.Union,
            null
        );
        Envelope.Freeze();
    }

    internal Geometry Fill { get; }

    internal Geometry Stroke { get; }

    internal Geometry Envelope { get; }

    internal Pen StrokePen { get; }

    private static StreamGeometry Build(HudContourFrame frame, bool strokeOnly)
    {

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {

            context.BeginFigure(Point(frame.Start), !strokeOnly, !strokeOnly || frame.Mode is Infrastructure.Configuration.HudDockMode.Free);

            foreach (var segment in frame.Segments)
                Append(context, segment, !strokeOnly || segment.IsStroked);
        }

        geometry.Freeze();

        return geometry;
    }

    private static void Append(StreamGeometryContext context, HudContourSegment segment, bool stroked)
    {

        if (segment.Control1 is { } first && segment.Control2 is { } second)
            context.BezierTo(
                Point(first),
                Point(second),
                Point(segment.End),
                stroked,
                true
            );
        else
            context.LineTo(Point(segment.End), stroked, true);
    }

    private static Point Point(HudContourPoint point)
        => new(point.X, point.Y);
}
