using AwesomeAssertions;
using System.Collections.Generic;
using TokenHound.App.Presentation;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies change notification and normalization of <see cref="HudScale"/> (TC-07).
/// </summary>
public sealed class HudScaleTests
{
    /// <summary>
    /// Verifies that a new size raises notifications for both properties and updates the factor.
    /// </summary>
    [Fact]
    public void Percent_WhenChanged_RaisesPercentAndFactor()
    {

        var sut = new HudScale();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Percent = 80;

        raised.Should().Equal(nameof(HudScale.Percent), nameof(HudScale.Factor));
        sut.Percent.Should().Be(80);
        sut.Factor.Should().Be(0.8);
    }

    /// <summary>
    /// Verifies that an unchanged or out-of-range value does not raise redundant notifications and stays clamped.
    /// </summary>
    [Fact]
    public void Percent_WhenUnchangedOrOutOfRange_RaisesNothingExtraAndClamps()
    {

        var sut = new HudScale();
        var raised = 0;
        sut.PropertyChanged += (_, _) => raised++;

        sut.Percent = 100;
        raised.Should().Be(0);

        sut.Percent = 900;

        sut.Percent.Should().Be(150);
        raised.Should().Be(2);
    }

    /// <summary>
    /// Verifies that a new instance starts at the default size.
    /// </summary>
    [Fact]
    public void NewInstance_StartsAtDefaultSize()
    {

        var sut = new HudScale();

        sut.Percent.Should().Be(100);
        sut.Factor.Should().Be(1.0);
    }
}
