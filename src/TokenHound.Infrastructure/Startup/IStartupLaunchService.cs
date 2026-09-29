namespace TokenHound.Infrastructure.Startup;

/// <summary>
/// Turns starting TokenHound at Windows sign-in on and off, reading the state from the operating system each time.
/// </summary>
public interface IStartupLaunchService
{
    /// <summary>
    /// Determines whether TokenHound starts at sign-in: the startup shortcut exists and Windows has not disabled it.
    /// </summary>
    /// <returns><see langword="true"/> when TokenHound starts at sign-in; otherwise <see langword="false"/>.</returns>
    bool IsEnabled();

    /// <summary>
    /// Writes the startup shortcut to <paramref name="executablePath"/> and clears a Windows "Startup apps" disabled flag.
    /// </summary>
    /// <param name="executablePath">The full path of the executable to start at sign-in.</param>
    void Enable(string executablePath);

    /// <summary>
    /// Deletes the startup shortcut, including one the installer created; does nothing when it is absent.
    /// </summary>
    void Disable();
}
