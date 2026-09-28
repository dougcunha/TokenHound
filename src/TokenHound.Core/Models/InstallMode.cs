namespace TokenHound.Core.Models;

/// <summary>
/// How the running copy of the application was deployed, which decides the update asset and apply mechanism.
/// </summary>
public enum InstallMode
{
    /// <summary>Unpacked from the framework-dependent zip; updated by swapping files in place.</summary>
    Portable,

    /// <summary>Installed by the Inno Setup installer; updated by running a newer installer.</summary>
    Installed
}
