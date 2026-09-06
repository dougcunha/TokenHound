using AwesomeAssertions;
using System;
using TokenHound.App.Interop;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Interop;

/// <summary>
/// Verifies the bitmask calculation and non-activating window styling helpers.
/// </summary>
public sealed class WindowStylesTests
{
    /// <summary>
    /// Verifies that applying extended styles to a zero initial bitmask sets all three required flags.
    /// </summary>
    [Fact]
    public void ApplyExtendedStyles_WhenInitialStyleIsZero_SetsAllRequiredFlags()
    {

        var result = WindowStyles.ApplyExtendedStyles(0);

        WindowStyles.HasNonActivatingStyles(result).Should().BeTrue();
        (result & WindowStyles.WS_EX_NOACTIVATE).Should().Be(WindowStyles.WS_EX_NOACTIVATE);
        (result & WindowStyles.WS_EX_TOOLWINDOW).Should().Be(WindowStyles.WS_EX_TOOLWINDOW);
        (result & WindowStyles.WS_EX_TOPMOST).Should().Be(WindowStyles.WS_EX_TOPMOST);
    }

    /// <summary>
    /// Verifies that applying extended styles preserves existing style bits.
    /// </summary>
    [Fact]
    public void ApplyExtendedStyles_WhenInitialStyleHasExistingBits_PreservesExistingBits()
    {

        const int existingStyle = 0x00000001 | 0x00010000;

        var result = WindowStyles.ApplyExtendedStyles(existingStyle);

        (result & existingStyle).Should().Be(existingStyle);
        WindowStyles.HasNonActivatingStyles(result).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HasNonActivatingStyles returns true when all flags are present, and false when any flag is missing.
    /// </summary>
    /// <param name="style">The bitmask style to test.</param>
    /// <param name="expected">The expected evaluation result.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(WindowStyles.WS_EX_NOACTIVATE, false)]
    [InlineData(WindowStyles.WS_EX_TOOLWINDOW, false)]
    [InlineData(WindowStyles.WS_EX_TOPMOST, false)]
    [InlineData(WindowStyles.WS_EX_NOACTIVATE | WindowStyles.WS_EX_TOOLWINDOW, false)]
    [InlineData(WindowStyles.WS_EX_NOACTIVATE | WindowStyles.WS_EX_TOPMOST, false)]
    [InlineData(WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST, false)]
    [InlineData(WindowStyles.WS_EX_NOACTIVATE | WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST, true)]
    [InlineData(WindowStyles.WS_EX_NOACTIVATE | WindowStyles.WS_EX_TOOLWINDOW | WindowStyles.WS_EX_TOPMOST | 0x1000, true)]
    public void HasNonActivatingStyles_EvaluatesCorrectly(int style, bool expected)
    {

        var result = WindowStyles.HasNonActivatingStyles(style);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that EnableNonActivating handles an IntPtr.Zero handle gracefully without throwing.
    /// </summary>
    [Fact]
    public void EnableNonActivating_WhenHandleIsZero_DoesNotThrow()
    {

        var act = () => WindowStyles.EnableNonActivating(IntPtr.Zero);

        act.Should().NotThrow();
    }
}
