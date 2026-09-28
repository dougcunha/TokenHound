namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// What started an update check; recorded in logs.
/// </summary>
public enum UpdateCheckTrigger
{
    /// <summary>The user chose "Check for Updates" in the tray menu.</summary>
    Manual,

    /// <summary>The periodic scheduler found a check due.</summary>
    Scheduled
}
