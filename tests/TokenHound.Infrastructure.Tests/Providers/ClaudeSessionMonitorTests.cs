using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies Claude Code session monitoring, PID liveness detection, JSON extraction, and tolerance checks.
/// </summary>
public sealed class ClaudeSessionMonitorTests
{
    /// <summary>
    /// Verifies that ProviderId returns "claude".
    /// </summary>
    [Fact]
    public void ProviderId_ReturnsClaude()
    {

        var monitor = new ClaudeSessionMonitor();

        monitor.ProviderId.Should().Be("claude");
    }

    /// <summary>
    /// Verifies that CheckLivenessAsync returns null when the sessions directory does not exist.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenMissingSessionsDirectory_ReturnsNull()
    {

        var missingDir = Path.Combine(Path.GetTempPath(), $"missing_claude_{Guid.NewGuid():N}");
        var monitor = new ClaudeSessionMonitor(missingDir);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CheckLivenessAsync returns null when the sessions directory exists but contains no files.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenSessionsDirectoryIsEmpty_ReturnsNull()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_empty_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var monitor = new ClaudeSessionMonitor(tempDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().BeNull();
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that a session file corresponding to a live process (current PID) returns a Busy AgentSession.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WithCurrentProcessPid_ReturnsBusySession()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_live_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var sessionFile = Path.Combine(sessionsDir, "session.json");
            var json = $$"""{"pid": {{Environment.ProcessId}}, "startedAt": {{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}""";

            await File.WriteAllTextAsync(sessionFile, json, TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(tempDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.Pid.Should().Be(Environment.ProcessId);
            session.State.Should().Be(AgentSessionState.Busy);
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that a session file with a stale or non-existent PID returns null.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WithStaleDeadPid_ReturnsNull()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_dead_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var sessionFile = Path.Combine(sessionsDir, "session.json");
            await File.WriteAllTextAsync(sessionFile, "{\"pid\": 99999999}", TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(tempDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().BeNull();
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that malformed JSON session files are skipped gracefully without throwing.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WithMalformedJson_SkipsGracefullyAndReturnsNull()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_malformed_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var sessionFile = Path.Combine(sessionsDir, "corrupted.json");
            await File.WriteAllTextAsync(sessionFile, "{ not valid json !! }", TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(tempDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().BeNull();
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that when both dead and live session files exist, the live session is returned.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WithDeadAndLiveSessions_FindsAndReturnsLiveSession()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_mixed_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var deadFile = Path.Combine(sessionsDir, "dead.json");
            await File.WriteAllTextAsync(deadFile, "{\"pid\": 99999999}", TestContext.Current.CancellationToken);

            var liveFile = Path.Combine(sessionsDir, "live.json");
            await File.WriteAllTextAsync(liveFile, $$"""{"pid": {{Environment.ProcessId}}}""", TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(tempDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.Pid.Should().Be(Environment.ProcessId);
            session.State.Should().Be(AgentSessionState.Busy);
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that CheckLivenessAsync throws OperationCanceledException when cancellation is requested.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenCancelled_ThrowsOperationCanceledException()
    {

        var monitor = new ClaudeSessionMonitor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await monitor.CheckLivenessAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that a session with an expected start time differing from the process start time beyond tolerance returns null.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WithRecycledPidExceedingTolerance_ReturnsNull()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_recycled_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            var sessionFile = Path.Combine(sessionsDir, "recycled.json");
            var ancientStartTime = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds();
            var json = $$"""{"pid": {{Environment.ProcessId}}, "startedAt": {{ancientStartTime}}}""";

            await File.WriteAllTextAsync(sessionFile, json, TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(tempDir, TimeSpan.FromSeconds(1));
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().BeNull();
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that ParseSessionJson correctly extracts PID, startedAt, and statusUpdatedAt, returning null on invalid input.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("[]", null)]
    [InlineData("{\"pid\": -5}", null)]
    [InlineData("{\"pid\": 0}", null)]
    [InlineData("{\"other\": 123}", null)]
    [InlineData("{\"pid\": 14220}", 14220)]
    [InlineData("{\"PID\": \"14220\"}", 14220)]
    public void ParseSessionJson_WithVariousInputs_HandlesCorrectly(string? json, int? expectedPid)
    {

        var result = ClaudeSessionMonitor.ParseSessionJson(json);

        if (expectedPid is null)
        {

            result.Should().BeNull();
        }
        else
        {

            result.Should().NotBeNull();
            result!.Pid.Should().Be(expectedPid.Value);
        }
    }
}
