using System;
using System.IO;
using System.IO.Compression;
using AwesomeAssertions;
using TokenHound.Infrastructure.Logging;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Logging;

/// <summary>
/// Verifies daily zip rotation, retention cleanup, and audit logging in <see cref="LogMaintenanceService"/>.
/// </summary>
public sealed class LogMaintenanceServiceTests : IDisposable
{
    private readonly string _testDirectory;

    /// <summary>
    /// Initializes test directory for maintenance validation.
    /// </summary>
    public LogMaintenanceServiceTests()
    {

        _testDirectory = Path.Combine(Path.GetTempPath(), "TokenHoundMaintTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_testDirectory))
        {

            try
            {

                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (Exception)
            {
            }
        }
    }

    /// <summary>
    /// Validates that prior day .logc files are compressed into .zip and originals deleted.
    /// </summary>
    [Fact]
    public void RunMaintenance_RotatesPriorDayLogcFiles()
    {

        var oldLogPath = Path.Combine(_testDirectory, "TokenHound_20260905.logc");
        File.WriteAllText(oldLogPath, "Prior log content");
        File.SetLastWriteTimeUtc(oldLogPath, DateTime.UtcNow.AddDays(-2));

        var todayLogPath = Path.Combine(_testDirectory, "TokenHound_20260907.logc");
        File.WriteAllText(todayLogPath, "Current active log content");
        File.SetLastWriteTimeUtc(todayLogPath, DateTime.UtcNow);

        var settings = new LogSettings
        {
            EnableDailyZipRotation = true,
            EnableMaintenanceAudit = true,
            EnableWeeklyZipCleanup = false,
            RetainedZipDays = 7
        };

        LogMaintenanceService.RunMaintenance(settings, _testDirectory, DateTime.UtcNow);

        var expectedZipPath = Path.Combine(_testDirectory, "TokenHound_20260905.zip");
        File.Exists(expectedZipPath).Should().BeTrue();
        File.Exists(oldLogPath).Should().BeFalse();
        File.Exists(todayLogPath).Should().BeTrue();

        using var archive = ZipFile.OpenRead(expectedZipPath);
        archive.Entries.Should().Contain(e => e.Name == "TokenHound_20260905.logc");

        var auditPath = Path.Combine(_testDirectory, settings.MaintenanceAuditFileName);
        File.Exists(auditPath).Should().BeTrue();
        var auditContent = File.ReadAllText(auditPath);
        auditContent.Should().Contain("Rotated and zipped");
    }

    /// <summary>
    /// Validates that zip files older than the retention threshold are purged.
    /// </summary>
    [Fact]
    public void RunMaintenance_PurgesExpiredZipFiles()
    {

        var expiredZip = Path.Combine(_testDirectory, "OldArchive.zip");
        File.WriteAllText(expiredZip, "dummy zip content");
        File.SetLastWriteTimeUtc(expiredZip, DateTime.UtcNow.AddDays(-10));

        var recentZip = Path.Combine(_testDirectory, "RecentArchive.zip");
        File.WriteAllText(recentZip, "dummy zip content");
        File.SetLastWriteTimeUtc(recentZip, DateTime.UtcNow.AddDays(-1));

        var settings = new LogSettings
        {
            EnableDailyZipRotation = false,
            EnableMaintenanceAudit = true,
            EnableWeeklyZipCleanup = false,
            RetainedZipDays = 7
        };

        LogMaintenanceService.RunMaintenance(settings, _testDirectory, DateTime.UtcNow);

        File.Exists(expiredZip).Should().BeFalse();
        File.Exists(recentZip).Should().BeTrue();

        var auditPath = Path.Combine(_testDirectory, settings.MaintenanceAuditFileName);
        var auditContent = File.ReadAllText(auditPath);
        auditContent.Should().Contain("Deleted expired zip archive");
    }
}
