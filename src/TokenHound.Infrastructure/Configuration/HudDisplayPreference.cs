namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted identity of the display chosen to host the docked HUD.
/// </summary>
public sealed record HudDisplayPreference
{
    /// <summary>
    /// Gets the monitor device path reported by the Windows display configuration, the primary match key.
    /// </summary>
    public string? DevicePath { get; init; }

    /// <summary>
    /// Gets the EDID manufacturer and product code as <c>MMMM:PPPP</c> hex, the fallback match key.
    /// </summary>
    public string? EdidKey { get; init; }

    /// <summary>
    /// Gets the friendly monitor name, used for display only.
    /// </summary>
    public string? Name { get; init; }
}
