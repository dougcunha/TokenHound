namespace TokenHound.App.ViewModels;

/// <summary>
/// Classifies a used fraction into the ring colour states of the usage notch design.
/// </summary>
public enum UsageLevel
{
    /// <summary>No used fraction is known; no bar is drawn.</summary>
    None,

    /// <summary>Below 50% used: plenty of room.</summary>
    Green,

    /// <summary>From 50% to below 80% used: getting close.</summary>
    Yellow,

    /// <summary>From 80% used upwards: nearly out or limit hit.</summary>
    Orange
}
