using System;
using System.IO;
using AwesomeAssertions;
using TokenHound.Infrastructure.Logging;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Logging;

/// <summary>
/// Verifies JSON deserialization and fallback behaviors of <see cref="LogConfigurationLoader"/>.
/// </summary>
public sealed class LogConfigurationLoaderTests
{
    private const string SAMPLE_CONFIG_JSON = """
    {
      "Log": {
        "ApplicationName": "{ApplicationName}",
        "LogBasePath": null,
        "LogFileNameTemplate": "{ApplicationName}_{Date}.logc",
        "LogSubFolderName": "{ApplicationName}",
        "MinimumLevel": "Debug",
        "FileSizeLimitBytes": 52428800,
        "RetainedFileCountLimit": 20,
        "RollingInterval": "Day",
        "RollOnFileSizeLimit": true,
        "EnableDailyZipRotation": true,
        "RetainedZipDays": 7,
        "EnableWeeklyZipCleanup": true,
        "WeeklyZipCleanupDay": "Sunday",
        "FileInUseRetryCount": 3,
        "FileInUseRetryDelayMilliseconds": 500,
        "EnableMaintenanceAudit": true,
        "MaintenanceAuditFileName": "log-maintenance.logc",
        "EnableConsole": false,
        "SharedFile": true,
        "OutputTemplate": "{Timestamp:dd-MM-yyyy HH:mm:ss.fff}|{SourceContext}|{MappedLevel,-5}|{ThreadId}|{Message:lj}{NewLine}{Exception}",
        "ConsoleTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}]{Scope} » {Message:lj}{NewLine}{Exception}",
        "MinimumLevelOverrides": {
          "Microsoft": "Warning",
          "System": "Warning"
        },
        "LogLevelMapping": {
          "Trace": "TRACE",
          "Debug": "DEBUG",
          "Information": "INFO",
          "Warning": "WARNING",
          "Error": "ERROR",
          "Fatal": "FATAL"
        },
        "GlobalProperties": {},
        "Enrichers": {
          "WithThreadId": true,
          "WithMachineName": true,
          "WithProcessId": false,
          "WithProcessName": false,
          "WithEnvironmentName": false,
          "WithEnvironmentUserName": false
        }
      }
    }
    """;

    /// <summary>
    /// Validates full deserialization of the user specification JSON.
    /// </summary>
    [Fact]
    public void FromJson_ParsesFullSpecificationCorrectly()
    {

        var settings = LogConfigurationLoader.FromJson(SAMPLE_CONFIG_JSON);

        settings.ApplicationName.Should().Be("{ApplicationName}");
        settings.LogBasePath.Should().BeNull();
        settings.LogFileNameTemplate.Should().Be("{ApplicationName}_{Date}.logc");
        settings.LogSubFolderName.Should().Be("{ApplicationName}");
        settings.MinimumLevel.Should().Be("Debug");
        settings.FileSizeLimitBytes.Should().Be(52428800L);
        settings.RetainedFileCountLimit.Should().Be(20);
        settings.RollingInterval.Should().Be("Day");
        settings.RollOnFileSizeLimit.Should().BeTrue();
        settings.EnableDailyZipRotation.Should().BeTrue();
        settings.RetainedZipDays.Should().Be(7);
        settings.EnableWeeklyZipCleanup.Should().BeTrue();
        settings.WeeklyZipCleanupDay.Should().Be("Sunday");
        settings.FileInUseRetryCount.Should().Be(3);
        settings.FileInUseRetryDelayMilliseconds.Should().Be(500);
        settings.EnableMaintenanceAudit.Should().BeTrue();
        settings.MaintenanceAuditFileName.Should().Be("log-maintenance.logc");
        settings.EnableConsole.Should().BeFalse();
        settings.SharedFile.Should().BeTrue();
        settings.MinimumLevelOverrides["Microsoft"].Should().Be("Warning");
        settings.LogLevelMapping["Debug"].Should().Be("DEBUG");
        settings.Enrichers.WithThreadId.Should().BeTrue();
        settings.Enrichers.WithMachineName.Should().BeTrue();
        settings.Enrichers.WithProcessId.Should().BeFalse();
    }

    /// <summary>
    /// Validates that invalid or missing file paths return defaults.
    /// </summary>
    [Fact]
    public void Load_MissingFile_ReturnsDefaultSettings()
    {

        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json");
        var settings = LogConfigurationLoader.Load(nonExistentPath);

        settings.Should().NotBeNull();
        settings.MinimumLevel.Should().Be("Debug");
        settings.RetainedZipDays.Should().Be(7);
        settings.EnableDailyZipRotation.Should().BeTrue();
    }
}
