using AwesomeAssertions;
using System;
using System.Linq;
using TokenHound.App.UI.Tray;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Tray;

/// <summary>
/// Verifies visibility transitions, redundant-call no-ops, the show/hide spies, the Notch "Hide" path,
/// and the one-way <see cref="NotchVisibilityController.AllowClose"/> latch (TC-03, TC-05, TC-09, TC-10).
/// </summary>
public sealed class NotchVisibilityControllerTests
{
    /// <summary>
    /// Verifies that toggling a visible controller hides then shows it, invoking each spy once per transition (TC-03).
    /// </summary>
    [Fact]
    public void Toggle_FromVisible_AlternatesVisibilityInvokingEachSpyOncePerTransition()
    {

        var showCount = 0;
        var hideCount = 0;
        var eventCount = 0;

        var sut = new NotchVisibilityController(() => showCount++, () => hideCount++);
        sut.VisibilityChanged += (_, _) => eventCount++;

        sut.Toggle();

        sut.IsNotchVisible.Should().BeFalse();
        hideCount.Should().Be(1);
        showCount.Should().Be(0);
        eventCount.Should().Be(1);

        sut.Toggle();

        sut.IsNotchVisible.Should().BeTrue();
        showCount.Should().Be(1);
        hideCount.Should().Be(1);
        eventCount.Should().Be(2);
    }

    /// <summary>
    /// Verifies that hiding an already-hidden controller invokes no delegate and raises no event (TC-03).
    /// </summary>
    [Fact]
    public void Hide_WhenAlreadyHidden_InvokesNoDelegateAndRaisesNoEvent()
    {

        var hideCount = 0;
        var eventCount = 0;

        var sut = new NotchVisibilityController(() => { }, () => hideCount++, initiallyVisible: false);
        sut.VisibilityChanged += (_, _) => eventCount++;

        sut.Hide();

        hideCount.Should().Be(0);
        eventCount.Should().Be(0);
        sut.IsNotchVisible.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that showing an already-visible controller invokes no delegate and raises no event (TC-03).
    /// </summary>
    [Fact]
    public void Show_WhenAlreadyVisible_InvokesNoDelegateAndRaisesNoEvent()
    {

        var showCount = 0;
        var eventCount = 0;

        var sut = new NotchVisibilityController(() => showCount++, () => { });
        sut.VisibilityChanged += (_, _) => eventCount++;

        sut.Show();

        showCount.Should().Be(0);
        eventCount.Should().Be(0);
        sut.IsNotchVisible.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that showing a hidden controller invokes the show spy exactly once (TC-05).
    /// </summary>
    [Fact]
    public void Show_FromHidden_InvokesShowSpyExactlyOnce()
    {

        var showCount = 0;
        var eventCount = 0;

        var sut = new NotchVisibilityController(() => showCount++, () => { }, initiallyVisible: false);
        sut.VisibilityChanged += (_, _) => eventCount++;

        sut.Show();

        showCount.Should().Be(1);
        eventCount.Should().Be(1);
        sut.IsNotchVisible.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the controller injects only show and hide delegates, exposing no activate or focus delegate (TC-05).
    /// </summary>
    [Fact]
    public void Constructor_InjectsOnlyShowAndHideDelegates()
    {

        var parameters = typeof(NotchVisibilityController).GetConstructors().Single().GetParameters();

        parameters.Select(static p => p.Name)
            .Should().Equal(new[] { "showNotch", "hideNotch", "initiallyVisible" });
        parameters.Count(static p => p.ParameterType == typeof(Action)).Should().Be(2);
    }

    /// <summary>
    /// Verifies that binding the Notch "Hide" path to <see cref="NotchVisibilityController.Hide"/> flips
    /// visibility through the hide spy exactly once, with no shutdown path involved (TC-09).
    /// </summary>
    [Fact]
    public void CloseInterceptedBoundToHide_FlipsVisibilityViaHideSpyOnce()
    {

        var hideCount = 0;

        var sut = new NotchVisibilityController(() => { }, () => hideCount++);
        Action closeIntercepted = sut.Hide;

        closeIntercepted();

        hideCount.Should().Be(1);
        sut.IsNotchVisible.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="NotchVisibilityController.ShouldInterceptClose"/> is true until
    /// <see cref="NotchVisibilityController.AllowClose"/> and then permanently false, even after further
    /// <see cref="NotchVisibilityController.AllowClose"/> or <see cref="NotchVisibilityController.Toggle"/> calls (TC-10).
    /// </summary>
    [Fact]
    public void ShouldInterceptClose_IsOneWay_TrueUntilAllowCloseThenAlwaysFalse()
    {

        var sut = new NotchVisibilityController(() => { }, () => { });

        sut.ShouldInterceptClose.Should().BeTrue();

        sut.AllowClose();

        sut.ShouldInterceptClose.Should().BeFalse();

        sut.AllowClose();
        sut.Toggle();

        sut.ShouldInterceptClose.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that while <see cref="NotchVisibilityController.ShouldInterceptClose"/> is true, the intercept
    /// branch invokes the hide spy exactly once (TC-10).
    /// </summary>
    [Fact]
    public void WhenInterceptingClose_HideInvokesHideSpyOnce()
    {

        var hideCount = 0;

        var sut = new NotchVisibilityController(() => { }, () => hideCount++);

        sut.ShouldInterceptClose.Should().BeTrue();

        sut.Hide();

        hideCount.Should().Be(1);
        sut.IsNotchVisible.Should().BeFalse();
    }
}
