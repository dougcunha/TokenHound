using System.Reflection;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Resolves the running application version from its assembly informational version (build metadata after <c>+</c> is dropped).
/// </summary>
public static class RunningVersion
{
    /// <summary>
    /// Reads and parses the informational version of <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The application assembly.</param>
    /// <returns>The parsed version, or <see langword="null"/> when the assembly carries no release version.</returns>
    public static ReleaseVersion? Resolve(Assembly assembly)
        => FromText(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>
    /// Parses an informational version text such as <c>1.2.3+abc123</c>.
    /// </summary>
    /// <param name="informationalVersion">The informational version text, or <see langword="null"/>.</param>
    /// <returns>The parsed version, or <see langword="null"/> when the text is not a release version.</returns>
    public static ReleaseVersion? FromText(string? informationalVersion)
        => ReleaseVersion.TryParse(informationalVersion, out var version) ? version : null;
}
