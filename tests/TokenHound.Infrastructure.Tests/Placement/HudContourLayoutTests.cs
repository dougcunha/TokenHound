using AwesomeAssertions;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>Checks contour boundaries independently of WPF rendering and native input.</summary>
public sealed class HudContourLayoutTests
{

    /// <summary>All modes retain the required body and a finite, closed contour at each content size.</summary>
    [Theory]
    [InlineData(HudDockMode.TopLeft)]
    [InlineData(HudDockMode.TopCenter)]
    [InlineData(HudDockMode.TopRight)]
    [InlineData(HudDockMode.LeftEdge)]
    [InlineData(HudDockMode.RightEdge)]
    [InlineData(HudDockMode.Free)]
    public void Create_PreservesBodyAndClosedBounds(HudDockMode mode)
    {

        foreach (var (width, height) in new[] { (1.0, 1.0), (16.0, 10.0), (48.0, 48.0), (360.0, 76.0) })
        {

            var frame = Create(mode, width, height);
            frame.BodyWidth.Should().BeApproximately(width, 1e-8);
            frame.BodyHeight.Should().BeApproximately(height, 1e-8);
            (frame.BodyOrigin.X >= 0 && frame.BodyOrigin.Y >= 0
                && frame.BodyOrigin.X + frame.BodyWidth <= frame.Width && frame.BodyOrigin.Y + frame.BodyHeight <= frame.Height).Should().BeTrue();
            frame.Segments[^1].End.Should().Be(frame.Start);
            var collection = Assert.IsAssignableFrom<IList<HudContourSegment>>(frame.Segments);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
            AssertSimple(frame);
            AssertTangents(frame);

            foreach (var point in AllPoints(frame))
            {

                double.IsFinite(point.X).Should().BeTrue();
                double.IsFinite(point.Y).Should().BeTrue();
                point.X.Should().BeInRange(0, frame.Width);
                point.Y.Should().BeInRange(0, frame.Height);
            }
        }
    }

    /// <summary>Opposite corners omit the unavailable continuation and stay flush to the work-area edge.</summary>
    [Fact]
    public void Create_TopCornersAreMirroredAndHaveOneJoin()
    {

        var left = Create(HudDockMode.TopLeft, 180, 48);
        var right = Create(HudDockMode.TopRight, 180, 48);
        left.BodyOrigin.X.Should().Be(0);
        (right.BodyOrigin.X + right.BodyWidth).Should().Be(right.Width);
        left.Segments.Count(static segment => !segment.IsStroked).Should().Be(2);
        right.Segments.Count(static segment => !segment.IsStroked).Should().Be(2);

        foreach (var point in AllPoints(left))
            AllPoints(right).Should().Contain(candidate => Math.Abs(candidate.X - (right.Width - point.X)) < 1e-8 && Math.Abs(candidate.Y - point.Y) < 1e-8);
    }

    /// <summary>Side contours are exact transforms of the canonical top definition.</summary>
    [Fact]
    public void Create_SidesShareTheTopCurveProfile()
    {

        var top = Create(HudDockMode.TopCenter, 180, 48);
        var left = Create(HudDockMode.LeftEdge, 48, 180);
        var right = Create(HudDockMode.RightEdge, 48, 180);
        var expected = AllPoints(top).ToArray();
        var leftPoints = AllPoints(left).ToArray();
        var rightPoints = AllPoints(right).ToArray();

        for (var index = 0; index < expected.Length; index++)
        {

            leftPoints[index].X.Should().Be(expected[index].Y);
            leftPoints[index].Y.Should().Be(expected[index].X);
            rightPoints[index].X.Should().Be(right.Width - expected[index].Y);
            rightPoints[index].Y.Should().Be(expected[index].X);
        }
    }

    /// <summary>The inverse join passes through the known quarter-circle midpoint.</summary>
    [Fact]
    public void Create_InverseJoinHasKnownBoundaryAndTangents()
    {

        var frame = Create(HudDockMode.TopCenter, 180, 48);
        var start = frame.Segments[0].End;
        var curve = frame.Segments[1];
        var midpoint = Evaluate(start, curve, 0.5);
        midpoint.X.Should().BeApproximately(start.X - 12 / Math.Sqrt(2), 1e-8);
        midpoint.Y.Should().BeApproximately(12 - 12 / Math.Sqrt(2), 1e-8);
        Assert.IsType<HudContourPoint>(curve.Control1).Y.Should().Be(start.Y);
        Assert.IsType<HudContourPoint>(curve.Control2).X.Should().Be(curve.End.X);
    }

    /// <summary>Free has four convex corners and a fully painted, closed stroke.</summary>
    [Fact]
    public void Create_FreeHasNoJoinsOrUnstrokedEdges()
    {

        var frame = Create(HudDockMode.Free, 180, 48);
        frame.Segments.Should().OnlyContain(static segment => segment.IsStroked);
        frame.Segments.Count(static segment => segment.Control1 is not null).Should().Be(4);
        frame.BodyOrigin.X.Should().Be(0.75);
        frame.BodyOrigin.Y.Should().Be(0.75);
        frame.Start.X.Should().Be(24.75);
        frame.Start.Y.Should().Be(0.75);
    }

    /// <summary>Continuations shrink before they could reduce provider space.</summary>
    [Fact]
    public void Create_ConstrainedHostBoundsJoinsWithoutShrinkingBody()
    {

        var request = Request(HudDockMode.TopCenter, 180, 48) with { Width = 185.5, Height = 48.75 };
        var frame = HudContourLayout.Create(request);
        var actual = Assert.IsType<HudContourFrame>(frame);
        actual.BodyWidth.Should().Be(180);
        actual.BodyOrigin.X.Should().Be(2.75);
        actual.BodyHeight.Should().Be(48);
    }

    /// <summary>Pre-arrange emptiness is explicit rather than a fabricated contour.</summary>
    [Theory]
    [InlineData(0, 50)]
    [InlineData(50, 0)]
    public void Create_ZeroArrangedExtentHasNoFrame(double width, double height)
    {

        var request = Request(HudDockMode.Free, 48, 48) with { Width = width, Height = height };
        HudContourLayout.Create(request).Should().BeNull();
        var empty = request with { BodyWidth = 0, BodyHeight = 0, Width = 120, Height = 40 };
        var arranged = Assert.IsType<HudContourFrame>(HudContourLayout.Create(empty));
        arranged.BodyWidth.Should().Be(118.5);
        arranged.BodyHeight.Should().Be(38.5);
    }

    /// <summary>Invalid numeric inputs fail at the geometry boundary.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_RejectsInvalidNumericInputs(double value)
    {

        var request = Request(HudDockMode.Free, 48, 48);
        HudContourLayout.Request[] invalid =
        [
            request with { BodyWidth = value }, request with { BodyHeight = value },
            request with { Width = value }, request with { Height = value }, request with { StrokeThickness = value }
        ];

        foreach (var input in invalid)
            Assert.Throws<ArgumentOutOfRangeException>(() => HudContourLayout.Create(input));
    }

    /// <summary>Unsupported modes and hosts that cannot contain content are rejected.</summary>
    [Fact]
    public void Create_RejectsUnknownModeAndUndersizedHost()
    {

        var request = Request(HudDockMode.Free, 48, 48) with { Width = 49.5, Height = 49.5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => HudContourLayout.Create(request with { Mode = (HudDockMode)99 }));
        Assert.Throws<ArgumentException>(() => HudContourLayout.Create(request with { Width = 40 }));
        Assert.Throws<ArgumentException>(() => HudContourLayout.Create(request with { Height = 40 }));
    }

    /// <summary>Extreme finite extents may not overflow desired dimensions.</summary>
    [Fact]
    public void Measure_RejectsOverflow()
    {

        var request = Request(HudDockMode.Free, double.MaxValue, 48) with { StrokeThickness = double.MaxValue };
        Assert.Throws<ArgumentOutOfRangeException>(() => HudContourLayout.Measure(request));
    }

    private static HudContourFrame Create(HudDockMode mode, double width, double height)
    {

        var request = Request(mode, width, height);
        var size = HudContourLayout.Measure(request);
        var frame = HudContourLayout.Create(request with { Width = size.X, Height = size.Y });

        return Assert.IsType<HudContourFrame>(frame);
    }

    private static HudContourLayout.Request Request(HudDockMode mode, double width, double height)
        => new() { Mode = mode, BodyWidth = width, BodyHeight = height, Width = 0, Height = 0 };

    private static IEnumerable<HudContourPoint> AllPoints(HudContourFrame frame)
    {

        yield return frame.Start;

        foreach (var segment in frame.Segments)
        {

            yield return segment.End;

            if (segment.Control1 is { } first)
                yield return first;

            if (segment.Control2 is { } second)
                yield return second;
        }
    }

    private static HudContourPoint Evaluate(HudContourPoint start, HudContourSegment segment, double time)
    {

        var first = Assert.IsType<HudContourPoint>(segment.Control1);
        var second = Assert.IsType<HudContourPoint>(segment.Control2);
        var inverse = 1 - time;

        return new HudContourPoint
        {

            X = inverse * inverse * inverse * start.X + 3 * inverse * inverse * time * first.X + 3 * inverse * time * time * second.X + time * time * time * segment.End.X,
            Y = inverse * inverse * inverse * start.Y + 3 * inverse * inverse * time * first.Y + 3 * inverse * time * time * second.Y + time * time * time * segment.End.Y
        };
    }

    private static void AssertSimple(HudContourFrame frame)
    {

        var points = Sample(frame).ToArray();

        for (var first = 0; first < points.Length - 1; first++)
        {

            for (var second = first + 2; second < points.Length - 1; second++)
            {

                var a = points[first];
                var b = points[first + 1];
                var c = points[second];
                var d = points[second + 1];
                var crossing = Cross(a, b, c) * Cross(a, b, d) < -1e-10 && Cross(c, d, a) * Cross(c, d, b) < -1e-10;
                crossing.Should().BeFalse("nonadjacent contour edges must not intersect");
            }
        }
    }

    private static IEnumerable<HudContourPoint> Sample(HudContourFrame frame)
    {

        var start = frame.Start;
        yield return start;

        foreach (var segment in frame.Segments)
        {

            if (segment.Control1 is not null)
            {

                for (var step = 1; step < 16; step++)
                    yield return Evaluate(start, segment, step / 16.0);
            }

            yield return segment.End;
            start = segment.End;
        }
    }

    private static double Cross(HudContourPoint a, HudContourPoint b, HudContourPoint c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static void AssertTangents(HudContourFrame frame)
    {

        for (var index = 0; index < frame.Segments.Count; index++)
        {

            var incoming = frame.Segments[index];
            var outgoing = frame.Segments[(index + 1) % frame.Segments.Count];

            if (!incoming.IsStroked || !outgoing.IsStroked)
                continue;

            var before = incoming.Control2 ?? (index == 0 ? frame.Start : frame.Segments[index - 1].End);
            var after = outgoing.Control1 ?? outgoing.End;
            Cross(before, incoming.End, after).Should().BeApproximately(0, 1e-8);
            var dot = (incoming.End.X - before.X) * (after.X - incoming.End.X)
                + (incoming.End.Y - before.Y) * (after.Y - incoming.End.Y);
            dot.Should().BeGreaterThanOrEqualTo(0, "exposed contour segments must join without reversing direction");
        }
    }
}
