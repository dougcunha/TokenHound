using System.Collections.Generic;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Configuration options governing file logging, rolling, retention, and maintenance.
/// </summary>
public sealed record LogSettings
{
    /// <summary>
    /// Default application name token.
    /// </summary>
    public const string APPLICATION_NAME_TOKEN = "{ApplicationName}";

    /// <summary>
    /// Default date token in file templates.
    /// </summary>
    public const string DATE_TOKEN = "{Date}";

    /// <summary>
    /// Default file extension for log files.
    /// </summary>
    public const string DEFAULT_LOG_EXTENSION = ".logc";

    /// <summary>
    /// Gets the application name or placeholder.
    /// </summary>
    public string ApplicationName { get; init; } = APPLICATION_NAME_TOKEN;

    /// <summary>
    /// Gets the base directory for logs. If null, defaults to the executable logs directory.
    /// </summary>
    public string? LogBasePath { get; init; }

    /// <summary>
    /// Gets the log file name template.
    /// </summary>
    public string LogFileNameTemplate { get; init; } = $"{APPLICATION_NAME_TOKEN}_{DATE_TOKEN}{DEFAULT_LOG_EXTENSION}";

    /// <summary>
    /// Gets the subfolder name under the base log directory.
    /// </summary>
    public string LogSubFolderName { get; init; } = APPLICATION_NAME_TOKEN;

    /// <summary>
    /// Gets the minimum log event level.
    /// </summary>
    public string MinimumLevel { get; init; } = "Debug";

    /// <summary>
    /// Gets the file size limit in bytes before rolling.
    /// </summary>
    public long FileSizeLimitBytes { get; init; } = 52428800;

    /// <summary>
    /// Gets the maximum retained log file count.
    /// </summary>
    public int RetainedFileCountLimit { get; init; } = 20;

    /// <summary>
    /// Gets the rolling interval (e.g., Day, Hour).
    /// </summary>
    public string RollingInterval { get; init; } = "Day";

    /// <summary>
    /// Gets a value indicating whether to roll on file size limit.
    /// </summary>
    public bool RollOnFileSizeLimit { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether daily zip rotation is enabled.
    /// </summary>
    public bool EnableDailyZipRotation { get; init; } = true;

    /// <summary>
    /// Gets the number of days to retain rotated zip archives.
    /// </summary>
    public int RetainedZipDays { get; init; } = 7;

    /// <summary>
    /// Gets a value indicating whether weekly zip cleanup is enabled.
    /// </summary>
    public bool EnableWeeklyZipCleanup { get; init; } = true;

    /// <summary>
    /// Gets the day of the week to trigger weekly zip cleanup.
    /// </summary>
    public string WeeklyZipCleanupDay { get; init; } = "Sunday";

    /// <summary>
    /// Gets the retry count when accessing locked log files.
    /// </summary>
    public int FileInUseRetryCount { get; init; } = 3;

    /// <summary>
    /// Gets the retry delay in milliseconds when accessing locked log files.
    /// </summary>
    public int FileInUseRetryDelayMilliseconds { get; init; } = 500;

    /// <summary>
    /// Gets a value indicating whether maintenance audit logging is enabled.
    /// </summary>
    public bool EnableMaintenanceAudit { get; init; } = true;

    /// <summary>
    /// Gets the maintenance audit log file name.
    /// </summary>
    public string MaintenanceAuditFileName { get; init; } = "log-maintenance.logc";

    /// <summary>
    /// Gets a value indicating whether console logging is enabled.
    /// </summary>
    public bool EnableConsole { get; init; }

    /// <summary>
    /// Gets a value indicating whether log files can be shared by multiple processes.
    /// </summary>
    public bool SharedFile { get; init; } = true;

    /// <summary>
    /// Gets the Serilog output template for file logging.
    /// </summary>
    public string OutputTemplate { get; init; } =
        "{Timestamp:dd-MM-yyyy HH:mm:ss.fff}|{SourceContext}|{MappedLevel,-5}|{ThreadId}|{Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Gets the Serilog console output template.
    /// </summary>
    public string ConsoleTemplate { get; init; } =
        "{Timestamp:HH:mm:ss} [{Level:u3}]{Scope} » {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Gets log level overrides by source namespace.
    /// </summary>
    public IReadOnlyDictionary<string, string> MinimumLevelOverrides { get; init; } =
        new Dictionary<string, string>
        {
            ["Microsoft"] = "Warning",
            ["System"] = "Warning"
        };

    /// <summary>
    /// Gets log level display mapping.
    /// </summary>
    public IReadOnlyDictionary<string, string> LogLevelMapping { get; init; } =
        new Dictionary<string, string>
        {
            ["Trace"] = "TRACE",
            ["Debug"] = "DEBUG",
            ["Information"] = "INFO",
            ["Warning"] = "WARNING",
            ["Error"] = "ERROR",
            ["Fatal"] = "FATAL"
        };

    /// <summary>
    /// Gets global structured log properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> GlobalProperties { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Gets the enricher settings.
    /// </summary>
    public LogEnricherSettings Enrichers { get; init; } = new();
}
