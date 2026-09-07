using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Configures Serilog pipeline, sinks, enrichers, and outputs application startup diagnostics.
/// </summary>
public static class LoggingBootstrapper
{
    private const int BANNER_LINE_LENGTH = 80;

    /// <summary>
    /// Initializes the Serilog logging pipeline based on the supplied or discovered configuration.
    /// </summary>
    /// <param name="settings">Optional log settings instance.</param>
    /// <param name="appName">Optional application name fallback.</param>
    /// <param name="baseDirectory">Optional base directory override.</param>
    /// <returns>The configured <see cref="ILogger"/> instance.</returns>
    public static ILogger Initialize(
        LogSettings? settings = null,
        string? appName = null,
        string? baseDirectory = null)
    {

        var effectiveSettings = settings ?? LogConfigurationLoader.Load(baseDirectory: baseDirectory);
        var effectiveAppName = LogPathResolver.ResolveApplicationName(effectiveSettings, appName);
        var logDir = LogPathResolver.ResolveLogDirectory(effectiveSettings, effectiveAppName, baseDirectory);

        Directory.CreateDirectory(logDir);

        var logFilePath = LogPathResolver.ResolveLogFilePathPattern(effectiveSettings, effectiveAppName, logDir);
        var config = BuildLoggerConfiguration(effectiveSettings, effectiveAppName, logFilePath);

        Log.Logger = config.CreateLogger();

        _ = Task.Run(() => LogMaintenanceService.RunMaintenance(effectiveSettings, logDir));

        return Log.Logger;
    }

    /// <summary>
    /// Writes the application startup banner and version information to the logger.
    /// </summary>
    /// <param name="logger">The target logger.</param>
    /// <param name="appName">The application name.</param>
    /// <param name="appVersion">The application version.</param>
    public static void LogHeader(
        ILogger logger,
        string appName,
        string appVersion)
    {

        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentException.ThrowIfNullOrWhiteSpace(appVersion);

        var separator = new string('=', BANNER_LINE_LENGTH);

        logger.Information(separator);
        logger.Information("  {ApplicationName} - Windows 11 LLM Usage & Rate Limit HUD", appName);
        logger.Information("  Version: {Version}", appVersion);
        logger.Information("  OS: {OSDescription} ({Architecture})", RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture);
        logger.Information("  Runtime: {FrameworkDescription}", RuntimeInformation.FrameworkDescription);
        logger.Information("  Directory: {BaseDirectory}", AppContext.BaseDirectory);
        logger.Information("  Process ID: {ProcessId}", Environment.ProcessId);
        logger.Information(separator);
    }

    private static LoggerConfiguration BuildLoggerConfiguration(
        LogSettings settings,
        string appName,
        string logFilePath)
    {

        var minLevel = ToLogEventLevel(settings.MinimumLevel);
        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel);

        ConfigureOverrides(config, settings.MinimumLevelOverrides);
        ConfigureEnrichers(config, settings, appName);
        ConfigureFileSink(config, settings, logFilePath, minLevel);

        if (settings.EnableConsole)
        {

            config.WriteTo.Console(
                outputTemplate: settings.ConsoleTemplate,
                restrictedToMinimumLevel: minLevel
            );
        }

        return config;
    }

    private static void ConfigureOverrides(
        LoggerConfiguration config,
        IReadOnlyDictionary<string, string> overrides)
    {

        foreach (var (source, level) in overrides)
        {

            config.MinimumLevel.Override(source, ToLogEventLevel(level));
        }
    }

    private static void ConfigureEnrichers(
        LoggerConfiguration config,
        LogSettings settings,
        string appName)
    {

        config.Enrich.With(new MappedLevelEnricher(settings.LogLevelMapping, appName));

        if (settings.Enrichers.WithThreadId)
            config.Enrich.WithThreadId();

        if (settings.Enrichers.WithMachineName)
            config.Enrich.WithMachineName();

        if (settings.Enrichers.WithProcessId)
            config.Enrich.WithProcessId();

        if (settings.Enrichers.WithProcessName)
            config.Enrich.WithProcessName();

        if (settings.Enrichers.WithEnvironmentName)
            config.Enrich.WithEnvironmentName();

        if (settings.Enrichers.WithEnvironmentUserName)
            config.Enrich.WithEnvironmentUserName();

        foreach (var (key, value) in settings.GlobalProperties)
        {

            config.Enrich.WithProperty(key, value);
        }
    }

    private static void ConfigureFileSink(
        LoggerConfiguration config,
        LogSettings settings,
        string logFilePath,
        LogEventLevel minLevel)
    {

        config.WriteTo.File(
            logFilePath,
            restrictedToMinimumLevel: minLevel,
            outputTemplate: settings.OutputTemplate,
            rollingInterval: ToRollingInterval(settings.RollingInterval),
            rollOnFileSizeLimit: settings.RollOnFileSizeLimit,
            fileSizeLimitBytes: settings.FileSizeLimitBytes,
            retainedFileCountLimit: settings.RetainedFileCountLimit,
            shared: settings.SharedFile
        );
    }

    private static LogEventLevel ToLogEventLevel(string levelName)
        => levelName?.ToLowerInvariant() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

    private static RollingInterval ToRollingInterval(string intervalName)
        => intervalName?.ToLowerInvariant() switch
        {
            "infinite" or "none" => RollingInterval.Infinite,
            "year" => RollingInterval.Year,
            "month" => RollingInterval.Month,
            "day" => RollingInterval.Day,
            "hour" => RollingInterval.Hour,
            "minute" => RollingInterval.Minute,
            _ => RollingInterval.Day
        };
}
