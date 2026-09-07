using System;
using System.Reflection;

namespace TokenHound.App.Presentation;

/// <summary>
/// Provides immutable application metadata and build version information.
/// </summary>
public sealed record ApplicationInfo
{
    /// <summary>
    /// The default application name.
    /// </summary>
    public const string DEFAULT_NAME = "TokenHound";

    /// <summary>
    /// The default application description.
    /// </summary>
    public const string DEFAULT_DESCRIPTION =
        "TokenHound monitors LLM usage, rate limits, and agent activity across AI coding tools on your Windows desktop.";

    /// <summary>
    /// Fallback value returned when version metadata is unavailable.
    /// </summary>
    public const string UNAVAILABLE_VERSION = "Version unavailable";

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the formatted build version string.
    /// </summary>
    public required string DisplayVersion { get; init; }

    /// <summary>
    /// Gets the application version, matching <see cref="DisplayVersion"/>.
    /// </summary>
    public string Version
        => DisplayVersion;

    /// <summary>
    /// Gets application metadata for the current running application.
    /// </summary>
    public static ApplicationInfo Current
        => FromAssembly(typeof(ApplicationInfo).Assembly);

    /// <summary>
    /// Reads application metadata from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to inspect, or null to inspect the current application assembly.</param>
    /// <returns>An immutable <see cref="ApplicationInfo"/> instance.</returns>
    public static ApplicationInfo Get(Assembly? assembly = null)
        => FromAssembly(assembly);

    /// <summary>
    /// Reads application metadata from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to inspect, or null to inspect the current application assembly.</param>
    /// <returns>An immutable <see cref="ApplicationInfo"/> instance.</returns>
    public static ApplicationInfo FromAssembly(Assembly? assembly = null)
    {

        var targetAssembly = assembly ?? typeof(ApplicationInfo).Assembly;
        var informationalVersion = targetAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        string displayVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            displayVersion = informationalVersion.Trim();
        }
        else
        {
            var assemblyVersion = targetAssembly.GetName().Version;

            displayVersion = IsValidVersion(assemblyVersion)
                ? assemblyVersion!.ToString().Trim()
                : UNAVAILABLE_VERSION;
        }

        return new ApplicationInfo
        {
            Name = DEFAULT_NAME,
            Description = DEFAULT_DESCRIPTION,
            DisplayVersion = displayVersion
        };
    }

    private static bool IsValidVersion(Version? version)
        => version is not null && (version.Major > 0 || version.Minor > 0 || version.Build > 0 || version.Revision > 0);
}
