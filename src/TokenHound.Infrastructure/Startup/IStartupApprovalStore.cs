namespace TokenHound.Infrastructure.Startup;

/// <summary>
/// Reads and clears the Windows "Startup apps" approval values of Startup-folder entries.
/// </summary>
public interface IStartupApprovalStore
{
    /// <summary>
    /// Reads the approval value of a Startup-folder entry.
    /// </summary>
    /// <param name="name">The entry file name, e.g. <c>TokenHound.lnk</c>.</param>
    /// <returns>The raw value, or <see langword="null"/> when Windows stores none.</returns>
    byte[]? ReadValue(string name);

    /// <summary>
    /// Deletes the approval value of a Startup-folder entry, which Windows then treats as enabled; absent values are ignored.
    /// </summary>
    /// <param name="name">The entry file name, e.g. <c>TokenHound.lnk</c>.</param>
    void DeleteValue(string name);
}
