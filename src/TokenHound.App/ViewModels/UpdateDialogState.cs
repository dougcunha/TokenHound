namespace TokenHound.App.ViewModels;

/// <summary>
/// The states the update dialog renders.
/// </summary>
public enum UpdateDialogState
{
    /// <summary>A check is running.</summary>
    Checking,

    /// <summary>The running version is current.</summary>
    UpToDate,

    /// <summary>A newer release is offered.</summary>
    Available,

    /// <summary>The offered release is downloading.</summary>
    Downloading,

    /// <summary>The downloaded release is being applied.</summary>
    Applying,

    /// <summary>Checks are blocked until a rate-limit deadline passes.</summary>
    RateLimited,

    /// <summary>The check or an action failed.</summary>
    Error
}
