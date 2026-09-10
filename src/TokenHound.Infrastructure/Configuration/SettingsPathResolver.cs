using System;
using System.IO;

namespace TokenHound.Infrastructure.Configuration;

internal static class SettingsPathResolver
{
    internal static string Resolve(
        string? filePath,
        string? baseDirectory,
        string defaultFileName)
    {

        if (!string.IsNullOrWhiteSpace(filePath) && Path.IsPathRooted(filePath))
            return filePath;

        var resolvedBaseDirectory = !string.IsNullOrWhiteSpace(baseDirectory)
            ? baseDirectory
            : AppContext.BaseDirectory;

        return Path.Combine(resolvedBaseDirectory, filePath ?? defaultFileName);
    }

    internal static string? ResolveOverride(
        string? filePath,
        string? baseDirectory,
        string defaultFileName)
        => string.IsNullOrWhiteSpace(filePath) && string.IsNullOrWhiteSpace(baseDirectory)
            ? null
            : Resolve(filePath, baseDirectory, defaultFileName);
}
