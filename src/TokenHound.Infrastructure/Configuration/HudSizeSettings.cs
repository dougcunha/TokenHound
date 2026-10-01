using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted HUD size, expressed as a percentage of the default capsule size.
/// </summary>
public sealed record HudSizeSettings
{
    /// <summary>
    /// The smallest accepted HUD size, in percent.
    /// </summary>
    public const int MINIMUM_PERCENT = 50;

    /// <summary>
    /// The largest accepted HUD size, in percent.
    /// </summary>
    public const int MAXIMUM_PERCENT = 150;

    /// <summary>
    /// The granularity of accepted HUD sizes, in percent.
    /// </summary>
    public const int STEP_PERCENT = 5;

    /// <summary>
    /// The HUD size used when nothing valid is persisted, in percent.
    /// </summary>
    public const int DEFAULT_PERCENT = 100;

    /// <summary>
    /// Gets the persisted HUD size in percent, or <see langword="null"/> to use the default.
    /// </summary>
    public int? Percent { get; init; }

    /// <summary>
    /// Gets the effective HUD size in percent, clamped to the accepted range and rounded to the accepted step.
    /// </summary>
    [JsonIgnore]
    public int ResolvedPercent
        => Normalize(Percent ?? DEFAULT_PERCENT);

    /// <summary>
    /// Gets the effective scale factor, where 1.0 is the default size.
    /// </summary>
    [JsonIgnore]
    public double Factor
        => ResolvedPercent / 100.0;

    /// <summary>
    /// Clamps a percentage to the accepted range and rounds it to the nearest accepted step.
    /// </summary>
    /// <param name="percent">The candidate HUD size in percent.</param>
    /// <returns>The nearest accepted HUD size in percent.</returns>
    public static int Normalize(int percent)
    {

        var stepped = (int)Math.Round(percent / (double)STEP_PERCENT, MidpointRounding.AwayFromZero) * STEP_PERCENT;

        return Math.Clamp(stepped, MINIMUM_PERCENT, MAXIMUM_PERCENT);
    }
}
