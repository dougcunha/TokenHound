using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Executes background maintenance, daily zip compression, and retention cleanup for log files.
/// </summary>
public static class LogMaintenanceService
{
    /// <summary>
    /// Runs all enabled log maintenance routines.
    /// </summary>
    /// <param name="settings">The logging configuration options.</param>
    /// <param name="logDirectory">The directory holding log files.</param>
    /// <param name="referenceTimeUtc">Optional reference UTC timestamp for testing.</param>
    public static void RunMaintenance(
        LogSettings settings,
        string logDirectory,
        DateTime? referenceTimeUtc = null)
    {

        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
            return;

        var referenceDate = referenceTimeUtc ?? DateTime.UtcNow;
        var auditPath = LogPathResolver.ResolveMaintenanceAuditPath(settings, logDirectory);

        if (settings.EnableDailyZipRotation)
            RotateDailyLogs(settings, logDirectory, auditPath, referenceDate);

        var shouldCleanupZips = !settings.EnableWeeklyZipCleanup ||
            string.Equals(referenceDate.DayOfWeek.ToString(), settings.WeeklyZipCleanupDay, StringComparison.OrdinalIgnoreCase);

        if (shouldCleanupZips && settings.RetainedZipDays > 0)
            CleanupExpiredZips(settings, logDirectory, auditPath, referenceDate);
    }

    private static void RotateDailyLogs(
        LogSettings settings,
        string logDirectory,
        string auditPath,
        DateTime referenceDate)
    {

        var logFiles = Directory.GetFiles(logDirectory, $"*{LogSettings.DEFAULT_LOG_EXTENSION}");

        foreach (var filePath in logFiles)
        {

            var fileName = Path.GetFileName(filePath);

            if (string.Equals(fileName, settings.MaintenanceAuditFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsFileEligibleForDailyRotation(filePath, referenceDate))
                continue;

            TryZipLogFile(settings, filePath, auditPath);
        }
    }

    private static bool IsFileEligibleForDailyRotation(string filePath, DateTime referenceDate)
    {

        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        return lastWriteTime.Date < referenceDate.Date;
    }

    private static void TryZipLogFile(
        LogSettings settings,
        string filePath,
        string auditPath)
    {

        var zipPath = Path.ChangeExtension(filePath, ".zip");
        var fileName = Path.GetFileName(filePath);

        var success = TryWithRetry(
            () =>
            {
                if (File.Exists(zipPath))
                {
                    using var updateArchive = ZipFile.Open(zipPath, ZipArchiveMode.Update);
                    var existingEntry = updateArchive.GetEntry(fileName);
                    existingEntry?.Delete();
                    updateArchive.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal);
                }
                else
                {
                    using var createArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                    createArchive.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal);
                }
            },
            settings.FileInUseRetryCount,
            settings.FileInUseRetryDelayMilliseconds
        );

        if (!success)
            return;

        var deleted = TryWithRetry(
            () => File.Delete(filePath),
            settings.FileInUseRetryCount,
            settings.FileInUseRetryDelayMilliseconds
        );

        if (deleted && settings.EnableMaintenanceAudit)
            AppendAudit(auditPath, $"Rotated and zipped: {fileName} -> {Path.GetFileName(zipPath)}");
    }

    private static void CleanupExpiredZips(
        LogSettings settings,
        string logDirectory,
        string auditPath,
        DateTime referenceDate)
    {

        var zipFiles = Directory.GetFiles(logDirectory, "*.zip");
        var expirationCutoff = referenceDate.AddDays(-settings.RetainedZipDays);

        foreach (var zipPath in zipFiles)
        {

            var lastWriteTime = File.GetLastWriteTimeUtc(zipPath);

            if (lastWriteTime >= expirationCutoff)
                continue;

            var fileName = Path.GetFileName(zipPath);
            var deleted = TryWithRetry(
                () => File.Delete(zipPath),
                settings.FileInUseRetryCount,
                settings.FileInUseRetryDelayMilliseconds
            );

            if (deleted && settings.EnableMaintenanceAudit)
                AppendAudit(auditPath, $"Deleted expired zip archive: {fileName} (age > {settings.RetainedZipDays} days)");
        }
    }

    private static bool TryWithRetry(
        Action action,
        int retryCount,
        int delayMilliseconds)
    {

        var attempts = Math.Max(1, retryCount);

        for (var i = 0; i < attempts; i++)
        {

            try
            {

                action();

                return true;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && i < attempts - 1)
            {

                if (delayMilliseconds > 0)
                    Thread.Sleep(delayMilliseconds);
            }
            catch (Exception)
            {

                return false;
            }
        }

        return false;
    }

    private static void AppendAudit(string auditPath, string message)
    {

        try
        {

            var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}|MAINTENANCE|{message}{Environment.NewLine}";
            File.AppendAllText(auditPath, entry);
        }
        catch (Exception)
        {
        }
    }
}
