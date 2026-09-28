using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies marker-based install mode detection and the writability probe of <see cref="InstallModeDetector"/> (TC-13).
/// </summary>
public sealed class InstallModeDetectorTests : IDisposable
{
    private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), $"install_mode_test_{Guid.NewGuid():N}");

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallModeDetectorTests"/> class.
    /// </summary>
    public InstallModeDetectorTests()
    {

        Directory.CreateDirectory(_directoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a folder without the marker is portable and with it is installed.
    /// </summary>
    [Fact]
    public void DetectMode_UsesInstallerMarker()
    {

        InstallModeDetector.DetectMode(_directoryPath).Should().Be(InstallMode.Portable);

        File.WriteAllText(Path.Combine(_directoryPath, InstallModeDetector.MARKER_FILE_NAME), "installed by setup");

        InstallModeDetector.DetectMode(_directoryPath).Should().Be(InstallMode.Installed);
    }

    /// <summary>
    /// Verifies that a writable folder passes the probe and the probe file does not remain.
    /// </summary>
    [Fact]
    public void IsWritable_WhenFolderAcceptsFiles_ReturnsTrueAndLeavesNoProbe()
    {

        InstallModeDetector.IsWritable(_directoryPath).Should().BeTrue();

        File.Exists(Path.Combine(_directoryPath, InstallModeDetector.PROBE_FILE_NAME)).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the probe fails when the probe file cannot be created, or the folder does not exist.
    /// </summary>
    [Fact]
    public void IsWritable_WhenProbeCannotBeCreated_ReturnsFalse()
    {

        Directory.CreateDirectory(Path.Combine(_directoryPath, InstallModeDetector.PROBE_FILE_NAME));

        InstallModeDetector.IsWritable(_directoryPath).Should().BeFalse();
        InstallModeDetector.IsWritable(Path.Combine(_directoryPath, "missing")).Should().BeFalse();
    }
}
