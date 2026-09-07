using System;
using System.IO;
using AwesomeAssertions;
using Serilog;
using TokenHound.Infrastructure.Logging;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Logging;

/// <summary>
/// Verifies Serilog bootstrap configuration, header emission, and .logc file output.
/// </summary>
public sealed class LoggingBootstrapperTests : IDisposable
{
    private readonly string _testBaseDir;

    /// <summary>
    /// Initializes temporary base directory for logging tests.
    /// </summary>
    public LoggingBootstrapperTests()
    {

        _testBaseDir = Path.Combine(Path.GetTempPath(), "TokenHoundBootTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testBaseDir);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        Log.CloseAndFlush();

        if (Directory.Exists(_testBaseDir))
        {

            try
            {

                Directory.Delete(_testBaseDir, recursive: true);
            }
            catch (Exception)
            {
            }
        }
    }

    /// <summary>
    /// Validates logger initialization, .logc file creation, and startup header emission.
    /// </summary>
    [Fact]
    public void Initialize_CreatesLogcFile_AndWritesHeader()
    {

        var settings = new LogSettings
        {
            LogBasePath = null,
            LogSubFolderName = "TokenHound",
            LogFileNameTemplate = "TokenHound_{Date}.logc",
            EnableDailyZipRotation = false,
            EnableConsole = false,
            SharedFile = true
        };

        var logger = LoggingBootstrapper.Initialize(settings, "TokenHound", _testBaseDir);
        LoggingBootstrapper.LogHeader(logger, "TokenHound", "1.2.3-test");
        logger.Information("Application startup probe verified.");

        Log.CloseAndFlush();

        var expectedFolder = Path.Combine(_testBaseDir, "logs", "TokenHound");
        Directory.Exists(expectedFolder).Should().BeTrue();

        var logcFiles = Directory.GetFiles(expectedFolder, "*.logc");
        logcFiles.Should().NotBeEmpty();

        var logContent = File.ReadAllText(logcFiles[0]);
        logContent.Should().Contain("TokenHound - Windows 11 LLM Usage & Rate Limit HUD");
        logContent.Should().Contain("Version: 1.2.3-test");
        logContent.Should().Contain("INFO");
        logContent.Should().Contain("Application startup probe verified.");
    }
}
