using System;
using System.Globalization;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Recognizes and decodes the free model limit message emitted by the Cline gateway.
/// </summary>
/// <remarks>
/// Cline publishes no free tier quota endpoint; the only signal is the inference failure text
/// <c>"... free limit reached on model X ... try again in 5 hours"</c>. The delay is prose, so a parsed
/// delay is derived evidence and never a reset timestamp from an authoritative source.
/// </remarks>
public static class ClineFreeModelLimit
{
    /// <summary>The case-insensitive marker that identifies a free model limit message.</summary>
    public const string LIMIT_MARKER = "free limit reached on model";

    /// <summary>The case-insensitive marker that introduces the retry delay.</summary>
    public const string RETRY_MARKER = "try again in ";

    private const int MAX_RETRY_TEXT_LENGTH = 64;

    private static readonly (string Unit, double Seconds)[] RETRY_UNITS =
    [
        ("second", 1.0),
        ("sec", 1.0),
        ("minute", 60.0),
        ("min", 60.0),
        ("hour", 3600.0),
        ("day", 86400.0)
    ];

    /// <summary>
    /// Reports whether the supplied text carries the Cline free model limit marker.
    /// </summary>
    /// <param name="text">The candidate message text.</param>
    /// <returns><see langword="true"/> when the marker is present.</returns>
    public static bool IsLimitMessage(string? text)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains(LIMIT_MARKER, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Decodes the retry delay published inside a free model limit message.
    /// </summary>
    /// <param name="text">The message text containing the retry marker.</param>
    /// <returns>The parsed delay, or <see langword="null"/> when the text carries no readable duration.</returns>
    public static TimeSpan? ParseRetryDelay(string? text)
    {

        if (string.IsNullOrWhiteSpace(text))
            return null;

        var markerIndex = text.IndexOf(RETRY_MARKER, StringComparison.OrdinalIgnoreCase);

        if (markerIndex < 0)
            return null;

        var start = markerIndex + RETRY_MARKER.Length;
        var length = Math.Min(MAX_RETRY_TEXT_LENGTH, text.Length - start);
        var segments = text.Substring(start, length).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var seconds = 0.0;
        var hasValue = false;

        for (var index = 0; index < segments.Length; index++)
        {

            var unit = ResolveUnit(segments[index]);

            if (unit is null)
                continue;

            if (TryParseQuantity(segments, index, out var quantity))
            {

                seconds += quantity * unit.Value;
                hasValue = true;
            }
        }

        return hasValue ? TimeSpan.FromSeconds(seconds) : null;
    }

    private static bool TryParseQuantity(string[] segments, int unitIndex, out double quantity)
    {

        quantity = 0.0;

        for (var index = unitIndex - 1; index >= 0; index--)
        {

            if (double.TryParse(segments[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var candidate))
            {

                quantity = candidate;

                return true;
            }

            if (!double.TryParse(segments[index], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                break;
        }

        return false;
    }

    private static double? ResolveUnit(string token)
    {

        foreach (var (unit, seconds) in RETRY_UNITS)
        {

            if (token.StartsWith(unit, StringComparison.OrdinalIgnoreCase))
                return seconds;
        }

        return null;
    }
}