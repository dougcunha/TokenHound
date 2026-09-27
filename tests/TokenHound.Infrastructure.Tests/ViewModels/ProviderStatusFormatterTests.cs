using AwesomeAssertions;
using System;
using System.Globalization;
using TokenHound.App.ViewModels;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies percentages, colour levels, reset lines, and comeback texts of <see cref="ProviderStatusFormatter"/>.
/// </summary>
public sealed class ProviderStatusFormatterTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 26, 22, 23, 0, TimeSpan.Zero);

    /// <summary>Verifies the HUD rounding of the used percentage.</summary>
    [Theory]
    [InlineData(0.21, "21%")]
    [InlineData(0.525, "52%")]
    [InlineData(1.0, "100%")]
    [InlineData(1.2, "120%")]
    public void FormatPercent_RoundsLikeTheHud(double usedFraction, string expected)
        => ProviderStatusFormatter.FormatPercent(usedFraction).Should().Be(expected);

    /// <summary>Verifies the ring colour states, including the thresholds and the unmeasured case.</summary>
    [Theory]
    [InlineData(0.21, UsageLevel.Green)]
    [InlineData(0.4999, UsageLevel.Green)]
    [InlineData(0.50, UsageLevel.Yellow)]
    [InlineData(0.52, UsageLevel.Yellow)]
    [InlineData(0.80, UsageLevel.Orange)]
    [InlineData(0.85, UsageLevel.Orange)]
    [InlineData(1.0, UsageLevel.Orange)]
    [InlineData(null, UsageLevel.None)]
    public void ResolveLevel_FollowsRingColourStates(double? usedFraction, UsageLevel expected)
        => ProviderStatusFormatter.ResolveLevel(usedFraction).Should().Be(expected);

    /// <summary>Verifies the relative and absolute parts of the reset line with the reference format.</summary>
    [Theory]
    [InlineData(37, "in 37 minutes · 09/26, 23:00")]
    [InlineData(65, "in 1 hour · 09/26, 23:28")]
    [InlineData(877, "in 14 hours · 09/27, 13:00")]
    [InlineData(4560, "in 3 days · 09/30, 02:23")]
    public void FormatResetLine_WhenResetPending_ShowsRelativeAndAbsolute(int minutes, string expected)
    {

        var formatter = CreateFormatter(CultureInfo.InvariantCulture);

        formatter.FormatResetLine(FIXED_NOW.AddMinutes(minutes)).Should().Be(expected);
    }

    /// <summary>Verifies that the absolute part follows the culture's day and month order and time format.</summary>
    [Fact]
    public void FormatResetLine_WhenPortugueseCulture_UsesDayMonthOrder()
    {

        var formatter = CreateFormatter(CultureInfo.GetCultureInfo("pt-BR"));

        formatter.FormatResetLine(FIXED_NOW.AddMinutes(877)).Should().Be("in 14 hours · 27/09, 13:00");
    }

    /// <summary>Verifies the texts without a pending reset and with a reset already due.</summary>
    [Fact]
    public void FormatResetLine_WhenNoResetOrPastReset_ShowsFixedTexts()
    {

        var formatter = CreateFormatter(CultureInfo.InvariantCulture);

        formatter.FormatResetLine(null).Should().Be("No reset pending");
        formatter.FormatResetLine(FIXED_NOW.AddMinutes(-1)).Should().Be("resetting now");
    }

    /// <summary>Verifies the comeback countdown of an exhausted account.</summary>
    [Theory]
    [InlineData(877, "back in 14h 37m")]
    [InlineData(4260, "back in 2d 23h")]
    [InlineData(37, "back in 37m")]
    public void FormatBackIn_WhenResetKnown_ShowsCompactCountdown(int minutes, string expected)
    {

        var formatter = CreateFormatter(CultureInfo.InvariantCulture);

        formatter.FormatBackIn(FIXED_NOW.AddMinutes(minutes)).Should().Be(expected);
    }

    /// <summary>Verifies that an exhausted window without a reset instant never invents one.</summary>
    [Fact]
    public void FormatBackIn_WhenResetUnknown_ShowsLimitReached()
        => CreateFormatter(CultureInfo.InvariantCulture).FormatBackIn(null).Should().Be("limit reached");

    private static ProviderStatusFormatter CreateFormatter(CultureInfo culture)
        => new(new ManualTimeProvider(FIXED_NOW), culture);
}
