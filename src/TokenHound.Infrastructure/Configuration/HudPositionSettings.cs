namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted screen placement of the HUD capsule, expressed in device-independent pixels.
/// </summary>
public sealed record HudPositionSettings
{
    /// <summary>
    /// Gets the horizontal offset of the HUD window, or <see langword="null"/> when never persisted.
    /// </summary>
    public double? Left { get; init; }

    /// <summary>
    /// Gets the vertical offset of the HUD window, or <see langword="null"/> when never persisted.
    /// </summary>
    public double? Top { get; init; }

    /// <summary>
    /// Attempts to read a usable placement from the persisted coordinates.
    /// </summary>
    /// <param name="left">Receives the horizontal offset when available.</param>
    /// <param name="top">Receives the vertical offset when available.</param>
    /// <returns><see langword="true"/> when both coordinates are present and finite.</returns>
    public bool TryGetPosition(out double left, out double top)
    {

        left = Left ?? double.NaN;
        top = Top ?? double.NaN;

        return double.IsFinite(left) && double.IsFinite(top);
    }
}
