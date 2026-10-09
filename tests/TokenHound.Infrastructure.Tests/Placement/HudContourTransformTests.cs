using AwesomeAssertions;
using TokenHound.App.UI.Placement;

namespace TokenHound.Infrastructure.Tests.Placement;

/// <summary>Verifies the production contour basis conversion independently of WPF.</summary>
public sealed class HudContourTransformTests
{

    /// <summary>Docking uses the same upward pixel coverage as a fractional SizeToContent window.</summary>
    [Theory]
    [InlineData(39.5, 1.5, 60)]
    [InlineData(101.5, 1.5, 153)]
    [InlineData(40, 1.25, 50)]
    [InlineData(40.2, 1.25, 51)]
    [InlineData(0, 1.5, 0)]
    public void PixelExtent_CoversTheWholeNativeWindow(double extent, double dpi, double expected)
    {

        HudContourTransform.PixelExtent(extent, dpi).Should().Be(expected);
    }

    /// <summary>Ancestor scale is already present and monitor DPI is applied exactly once.</summary>
    [Theory]
    [InlineData(0.8, 1.5, 1.0, 1.2, 18.0)]
    [InlineData(1.0, 1.25, 1.5, 0.8333333333333333, 10.0)]
    [InlineData(1.25, 1.0, 1.25, 1.0, 9.6)]
    [InlineData(1.5, 1.5, 1.5, 1.5, 12.0)]
    public void ForDpi_AppliesScaleAndTranslationOnce(
        double hudScale,
        double ownerDpi,
        double shadowDpi,
        double expectedBasis,
        double expectedOffset
    )
    {

        var basis = Basis(hudScale);
        var converted = basis.ForDpi(Point(ownerDpi, ownerDpi), Point(shadowDpi, shadowDpi));

        converted.Horizontal.X.Should().BeApproximately(expectedBasis, 1e-10);
        converted.Vertical.Y.Should().BeApproximately(expectedBasis, 1e-10);
        converted.Origin.X.Should().BeApproximately(expectedOffset, 1e-10);
        converted.Origin.Y.Should().BeApproximately(-expectedOffset / 2, 1e-10);
        converted.Horizontal.Y.Should().Be(0);
        converted.Vertical.X.Should().Be(0);
        basis.Should().Be(Basis(hudScale));
    }

    /// <summary>Rotation and shear cross terms use the output axis DPI rather than the input axis.</summary>
    [Fact]
    public void ForDpi_PreservesTransformedAxesWithDifferentAxisDpi()
    {

        var basis = new HudContourTransform
        {
            Origin = Point(-12, 8),
            Horizontal = Point(0, 0.8),
            Vertical = Point(-0.8, 0.2)
        };
        var converted = basis.ForDpi(Point(1.5, 1.25), Point(1, 1));

        converted.Origin.Should().Be(Point(-18, 10));
        converted.Horizontal.Should().Be(Point(0, 1));
        converted.Vertical.X.Should().BeApproximately(-1.2, 1e-10);
        converted.Vertical.Y.Should().Be(0.25);
    }

    /// <summary>The same arranged frame produces equal values for unchanged-frame suppression.</summary>
    [Fact]
    public void ForDpi_UnchangedInputsRemainEqualAndDpiChangesDiffer()
    {

        var basis = Basis(0.8);
        var first = basis.ForDpi(Point(1.5, 1.5), Point(1, 1));
        var second = basis.ForDpi(Point(1.5, 1.5), Point(1, 1));
        var changed = basis.ForDpi(Point(1.25, 1.25), Point(1, 1));

        first.Should().Be(second);
        first.Should().NotBe(changed);
        basis.ForDpi(Point(1.25, 1.25), Point(1.25, 1.25)).Should().Be(basis);
    }

    /// <summary>Shared native bounds keep a negative display origin outside the local DPI conversion.</summary>
    [Fact]
    public void ForDpi_AlignsPhysicalPointsAtNegativeDisplayOrigin()
    {

        var converted = Basis(0.8).ForDpi(Point(1.25, 1.25), Point(1.5, 1.5));
        var physicalX = -1920 + (converted.Origin.X + 30 * converted.Horizontal.X) * 1.5;
        var physicalY = -1440 + (converted.Origin.Y + 20 * converted.Vertical.Y) * 1.5;

        physicalX.Should().BeApproximately(-1875, 1e-10);
        physicalY.Should().BeApproximately(-1427.5, 1e-10);
    }

    /// <summary>Invalid DPI is rejected instead of publishing a nonfinite shadow frame.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ForDpi_RejectsInvalidDpi(double invalid)
    {

        var basis = Basis(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => basis.ForDpi(Point(invalid, 1), Point(1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => basis.ForDpi(Point(1, invalid), Point(1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => basis.ForDpi(Point(1, 1), Point(invalid, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => basis.ForDpi(Point(1, 1), Point(1, invalid)));
    }

    private static HudContourTransform Basis(double scale)
        => new()
        {
            Origin = Point(12, -6),
            Horizontal = Point(scale, 0),
            Vertical = Point(0, scale)
        };

    private static HudContourPoint Point(double x, double y)
        => new() { X = x, Y = y };
}
