using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using TokenHound.Infrastructure.Providers.Copilot;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies Copilot host process and VS Code extension qualification.
/// </summary>
public sealed class CopilotProcessHostDetectorTests
{
    /// <summary>
    /// Verifies that a live copilot process qualifies without a VS Code extension.
    /// </summary>
    [Fact]
    public void HasQualifyingHost_WhenCopilotProcessIsLive_ReturnsTrue()
    {
        var detector = CreateDetector("copilot", processLiveness: static _ => true);

        detector.HasQualifyingHost().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that gh is accepted as a live Copilot-capable host.
    /// </summary>
    [Fact]
    public void HasQualifyingHost_WhenGhProcessIsLive_ReturnsTrue()
    {
        var detector = CreateDetector("gh.exe", processLiveness: static _ => true);

        detector.TryGetQualifyingHost(out var host).Should().BeTrue();
        host!.Name.Should().Be("gh.exe");
    }

    /// <summary>
    /// Verifies that Code.exe requires an installed GitHub Copilot extension.
    /// </summary>
    [Fact]
    public void HasQualifyingHost_WhenCodeHasNoCopilotExtension_ReturnsFalse()
    {
        var root = CreateDirectory();

        try
        {
            var detector = CreateDetector(
                "Code.exe",
                root,
                processLiveness: static _ => true
            );

            detector.HasQualifyingHost().Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// Verifies that Code.exe qualifies when a supported Copilot extension directory exists.
    /// </summary>
    [Fact]
    public void HasQualifyingHost_WhenCodeHasCopilotExtension_ReturnsTrue()
    {
        var root = CreateDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "github.copilot-chat-1.0.0"));
            var detector = CreateDetector(
                "Code.exe",
                root,
                processLiveness: static _ => true
            );

            detector.HasCopilotExtension().Should().BeTrue();
            detector.HasQualifyingHost().Should().BeTrue();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// Verifies that a dead process does not produce a qualifying host.
    /// </summary>
    [Fact]
    public void HasQualifyingHost_WhenProcessIsNotLive_ReturnsFalse()
    {
        var detector = CreateDetector("copilot", processLiveness: static _ => false);

        detector.HasQualifyingHost().Should().BeFalse();
    }

    private static CopilotProcessHostDetector CreateDetector(
        string processName,
        string? extensionRoot = null,
        Func<CopilotProcessHostDetector.ProcessInfo, bool>? processLiveness = null)
    {

        return new CopilotProcessHostDetector(
            processEnumerator: () =>
            [
                new CopilotProcessHostDetector.ProcessInfo
                {
                    Name = processName,
                    Pid = 42,
                    StartTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
                }
            ],
            processLiveness: processLiveness ?? (static _ => true),
            extensionDirectories: extensionRoot is null ? [] : [extensionRoot]
        );
    }

    private static string CreateDirectory()
    {

        var directory = Path.Combine(Path.GetTempPath(), $"copilot-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        return directory;
    }
}
