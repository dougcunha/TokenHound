using System;
using System.IO;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Resolves and formats directories and file templates for Serilog and maintenance tasks.
/// </summary>
public static class LogPathResolver
{
    /// <summary>
    /// Default folder name for application logs.
    /// </summary>
    public const string DEFAULT_LOGS_FOLDER = "logs";

    /// <summary>
    /// Resolves the effective application name from settings.
    /// </summary>
    /// <param name="settings">The log settings instance.</param>
    /// <param name="fallbackName">Fallback name when unspecified or templated.</param>
    /// <returns>The resolved application name.</returns>
    public static string ResolveApplicationName(LogSettings settings, string? fallbackName = null)
    {

        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.ApplicationName) ||
            string.Equals(settings.ApplicationName, LogSettings.APPLICATION_NAME_TOKEN, StringComparison.OrdinalIgnoreCase))
            return fallbackName ?? "TokenHound";

        return settings.ApplicationName.Trim();
    }

    /// <summary>
    /// Resolves the absolute directory path where logs will be stored.
    /// </summary>
    /// <param name="settings">The log settings instance.</param>
    /// <param name="appName">The resolved application name.</param>
    /// <param name="baseDirectory">Optional base directory override.</param>
    /// <returns>The absolute path to the log directory.</returns>
    public static string ResolveLogDirectory(
        LogSettings settings,
        string appName,
        string? baseDirectory = null)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        var basePath = !string.IsNullOrWhiteSpace(settings.LogBasePath)
            ? settings.LogBasePath
            : Path.Combine(baseDirectory ?? AppContext.BaseDirectory, DEFAULT_LOGS_FOLDER);

        if (string.IsNullOrWhiteSpace(settings.LogSubFolderName))
            return Path.GetFullPath(basePath);

        var subFolder = settings.LogSubFolderName.Replace(
            LogSettings.APPLICATION_NAME_TOKEN,
            appName,
            StringComparison.OrdinalIgnoreCase
        );

        return Path.GetFullPath(Path.Combine(basePath, subFolder));
    }

    /// <summary>
    /// Resolves the file path pattern passed to Serilog rolling file sink.
    /// </summary>
    /// <param name="settings">The log settings instance.</param>
    /// <param name="appName">The resolved application name.</param>
    /// <param name="logDirectory">The resolved log directory.</param>
    /// <returns>The file path pattern for Serilog.</returns>
    public static string ResolveLogFilePathPattern(
        LogSettings settings,
        string appName,
        string logDirectory)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        var template = settings.LogFileNameTemplate;

        if (string.IsNullOrWhiteSpace(template))
            template = $"{LogSettings.APPLICATION_NAME_TOKEN}_{LogSettings.DATE_TOKEN}{LogSettings.DEFAULT_LOG_EXTENSION}";

        var fileName = template
            .Replace(LogSettings.APPLICATION_NAME_TOKEN, appName, StringComparison.OrdinalIgnoreCase)
            .Replace(LogSettings.DATE_TOKEN, string.Empty, StringComparison.OrdinalIgnoreCase);

        if (!fileName.EndsWith(LogSettings.DEFAULT_LOG_EXTENSION, StringComparison.OrdinalIgnoreCase))
            fileName += LogSettings.DEFAULT_LOG_EXTENSION;

        return Path.Combine(logDirectory, fileName);
    }

    /// <summary>
    /// Resolves the full path to the maintenance audit file.
    /// </summary>
    /// <param name="settings">The log settings instance.</param>
    /// <param name="logDirectory">The resolved log directory.</param>
    /// <returns>The full path to the maintenance audit file.</returns>
    public static string ResolveMaintenanceAuditPath(LogSettings settings, string logDirectory)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        var auditFile = !string.IsNullOrWhiteSpace(settings.MaintenanceAuditFileName)
            ? settings.MaintenanceAuditFileName
            : "log-maintenance.logc";

        return Path.Combine(logDirectory, auditFile);
    }
}
