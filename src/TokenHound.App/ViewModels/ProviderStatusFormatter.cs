using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Formats percentages, colour levels, reset lines, and comeback countdowns for the provider status window.
/// </summary>
public sealed partial class ProviderStatusFormatter
{
    private const double YELLOW_THRESHOLD = 0.50;
    private const double ORANGE_THRESHOLD = 0.80;
    private const string NO_RESET_TEXT = "No reset pending";
    private const string RESETTING_NOW_TEXT = "resetting now";
    private const string LIMIT_REACHED_TEXT = "limit reached";
    private static readonly char[] DATE_SEPARATORS = [' ', '/', '-', '.'];

    private readonly CultureInfo _culture;
    private readonly string _monthDayPattern;

    /// <summary>Initializes a new instance of the <see cref="ProviderStatusFormatter"/> class.</summary>
    /// <param name="timeProvider">Clock and local time zone, or null for the system clock.</param>
    /// <param name="culture">Culture for absolute dates and times, or null for the current culture.</param>
    public ProviderStatusFormatter(TimeProvider? timeProvider = null, CultureInfo? culture = null)
    {

        TimeProvider = timeProvider ?? TimeProvider.System;
        _culture = culture ?? CultureInfo.CurrentCulture;
        _monthDayPattern = ResolveMonthDayPattern(_culture.DateTimeFormat.ShortDatePattern);
    }

    /// <summary>Gets the clock used for countdowns and local times.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Formats a used fraction as a whole percentage with the HUD rounding.</summary>
    /// <param name="usedFraction">The used fraction.</param>
    /// <returns>The percentage text, e.g. <c>63%</c>.</returns>
    public static string FormatPercent(double usedFraction)
        => $"{(int)Math.Round(usedFraction * 100.0)}%";

    /// <summary>Classifies a used fraction into a ring colour level.</summary>
    /// <param name="usedFraction">The used fraction, or null when unmeasured.</param>
    /// <returns>The colour level; <see cref="UsageLevel.None"/> when unmeasured.</returns>
    public static UsageLevel ResolveLevel(double? usedFraction)
        => usedFraction switch
        {
            null => UsageLevel.None,
            < YELLOW_THRESHOLD => UsageLevel.Green,
            < ORANGE_THRESHOLD => UsageLevel.Yellow,
            _ => UsageLevel.Orange
        };

    /// <summary>Formats the reset line with a relative countdown and the absolute local date and time.</summary>
    /// <param name="resetTimeUtc">The reset instant, or null when no reset is pending.</param>
    /// <returns>The reset line, e.g. <c>in 14 hours · 09/27, 13:00</c>.</returns>
    public string FormatResetLine(DateTimeOffset? resetTimeUtc)
    {

        if (resetTimeUtc is not { } reset)
            return NO_RESET_TEXT;

        var remaining = reset - TimeProvider.GetUtcNow();

        if (remaining <= TimeSpan.Zero)
            return RESETTING_NOW_TEXT;

        return $"in {FormatRelative(remaining)} · {FormatAbsolute(reset)}";
    }

    /// <summary>Formats the comeback countdown of an exhausted account.</summary>
    /// <param name="resetTimeUtc">The earliest reset instant of the exhausted windows, or null if unknown.</param>
    /// <returns>The comeback text, e.g. <c>back in 14h 37m</c>, or <c>limit reached</c> without a reset.</returns>
    public string FormatBackIn(DateTimeOffset? resetTimeUtc)
    {

        if (resetTimeUtc is not { } reset)
            return LIMIT_REACHED_TEXT;

        var remaining = reset - TimeProvider.GetUtcNow();

        if (remaining <= TimeSpan.Zero)
            return RESETTING_NOW_TEXT;

        if (remaining.TotalDays >= 1.0)
            return $"back in {(int)remaining.TotalDays}d {remaining.Hours}h";

        if (remaining.TotalHours >= 1.0)
            return $"back in {(int)remaining.TotalHours}h {remaining.Minutes}m";

        return $"back in {Math.Max(1, (int)remaining.TotalMinutes)}m";
    }

    private static string FormatRelative(TimeSpan remaining)
    {

        if (remaining.TotalHours < 1.0)
            return Pluralize(Math.Max(1, (int)remaining.TotalMinutes), "minute");

        if (remaining.TotalDays < 1.0)
            return Pluralize((int)remaining.TotalHours, "hour");

        return Pluralize((int)remaining.TotalDays, "day");
    }

    private static string Pluralize(int count, string unit)
        => count == 1 ? $"1 {unit}" : $"{count} {unit}s";

    private string FormatAbsolute(DateTimeOffset resetTimeUtc)
    {

        var local = TimeZoneInfo.ConvertTime(resetTimeUtc, TimeProvider.LocalTimeZone);
        var date = local.ToString(_monthDayPattern, _culture);
        var time = local.ToString(_culture.DateTimeFormat.ShortTimePattern, _culture);

        return $"{date}, {time}";
    }

    private static string ResolveMonthDayPattern(string shortDatePattern)
    {

        var withoutYear = YearTokenRegex().Replace(shortDatePattern, string.Empty);
        var trimmed = withoutYear.Trim(DATE_SEPARATORS);
        var paddedMonth = SingleMonthRegex().Replace(trimmed, "MM");

        return SingleDayRegex().Replace(paddedMonth, "dd");
    }

    [GeneratedRegex("[^dMy]*y+")]
    private static partial Regex YearTokenRegex();

    [GeneratedRegex("(?<!M)M(?!M)")]
    private static partial Regex SingleMonthRegex();

    [GeneratedRegex("(?<!d)d(?!d)")]
    private static partial Regex SingleDayRegex();
}
