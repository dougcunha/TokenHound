using System;
using System.IO;
using AwesomeAssertions;
using TokenHound.Infrastructure.Logging;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Logging;

/// <summary>
/// Verifies path resolution and token substitution rules in <see cref="LogPathResolver"/>.
/// </summary>
public sealed class LogPathResolverTests
{
    /// <summary>
    /// Validates application name resolution and placeholder substitution.
    /// </summary>
    [Fact]
    public void ResolveApplicationName_SubstitutesPlaceholder()
    {

        var placeholderSettings = new LogSettings { ApplicationName = "{ApplicationName}" };
        var name1 = LogPathResolver.ResolveApplicationName(placeholderSettings, "CustomApp");

        name1.Should().Be("CustomApp");

        var explicitSettings = new LogSettings { ApplicationName = "ExplicitApp" };
        var name2 = LogPathResolver.ResolveApplicationName(explicitSettings, "Fallback");

        name2.Should().Be("ExplicitApp");
    }

    /// <summary>
    /// Validates log directory resolution under the base directory with subfolder.
    /// </summary>
    [Fact]
    public void ResolveLogDirectory_UsesBaseDirectoryAndSubFolder()
    {

        var tempBase = Path.Combine(Path.GetTempPath(), "TokenHoundBaseTest");
        var settings = new LogSettings
        {
            LogBasePath = null,
            LogSubFolderName = "{ApplicationName}"
        };

        var resolvedDir = LogPathResolver.ResolveLogDirectory(settings, "TokenHound", tempBase);
        var expectedDir = Path.GetFullPath(Path.Combine(tempBase, "logs", "TokenHound"));

        resolvedDir.Should().Be(expectedDir);
    }

    /// <summary>
    /// Validates file path pattern resolution and preservation of .logc extension.
    /// </summary>
    [Fact]
    public void ResolveLogFilePathPattern_ProducesLogcExtensionPattern()
    {

        var settings = new LogSettings
        {
            LogFileNameTemplate = "{ApplicationName}_{Date}.logc"
        };
        var targetDir = Path.Combine(Path.GetTempPath(), "Logs");

        var pattern = LogPathResolver.ResolveLogFilePathPattern(settings, "TokenHound", targetDir);

        pattern.Should().EndWith(".logc");
        pattern.Should().Contain("TokenHound_");
        pattern.Should().NotContain("{ApplicationName}");
        pattern.Should().NotContain("{Date}");
    }

    /// <summary>
    /// Validates maintenance audit file path resolution.
    /// </summary>
    [Fact]
    public void ResolveMaintenanceAuditPath_CombinesDirectoryAndFileName()
    {

        var settings = new LogSettings
        {
            MaintenanceAuditFileName = "log-maintenance.logc"
        };
        var targetDir = Path.Combine(Path.GetTempPath(), "Logs");

        var auditPath = LogPathResolver.ResolveMaintenanceAuditPath(settings, targetDir);

        auditPath.Should().Be(Path.Combine(targetDir, "log-maintenance.logc"));
    }
}
