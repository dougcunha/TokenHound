using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Copilot;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies Copilot file freshness, host conjunction, debounce, and disposal behavior.
/// </summary>
public sealed class CopilotActivityMonitorTests
{
    /// <summary>
    /// Verifies that a recent read-only write and live host produce Busy without changing metadata.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenWriteAndHostAreRecent_ReturnsBusyWithoutMutation()
    {
        var root = CreateDirectory();
        var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
        var file = CreateSessionFile(root, "events.jsonl", "original");
        File.SetLastWriteTimeUtc(file, now.AddSeconds(-5).UtcDateTime);
        var originalContent = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
        var originalWriteTime = File.GetLastWriteTimeUtc(file);
        var monitor = CreateMonitor(root, now, "copilot", true, enableWatcher: false);

        try
        {
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(TokenHound.Core.Models.AgentSessionState.Busy);
            var currentContent = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            currentContent.Should().Be(originalContent);
            File.GetLastWriteTimeUtc(file).Should().Be(originalWriteTime);
        }
        finally
        {
            monitor.Dispose();
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// Verifies that a recent file without a qualifying host remains idle.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenHostIsMissing_ReturnsIdle()
    {
        var root = CreateDirectory();
        var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
        var file = CreateSessionFile(root, "events.jsonl", "original");
        File.SetLastWriteTimeUtc(file, now.AddSeconds(-5).UtcDateTime);
        using var monitor = CreateMonitor(root, now, "other", false, enableWatcher: false);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
        Directory.Delete(root, true);
    }

    /// <summary>
    /// Verifies that a live host without a recent activity write remains idle.
    /// </summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenWriteIsOlderThanThirtySeconds_ReturnsIdle()
    {
        var root = CreateDirectory();
        var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
        var file = CreateSessionFile(root, "events.jsonl", "original");
        File.SetLastWriteTimeUtc(file, now.AddSeconds(-31).UtcDateTime);
        using var monitor = CreateMonitor(root, now, "copilot", true, enableWatcher: false);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
        Directory.Delete(root, true);
    }

    /// <summary>
    /// Verifies that watcher notifications within the debounce interval consolidate to one event.
    /// </summary>
    [Fact]
    public async Task Watcher_WhenWritesBurst_ConsolidatesNotifications()
    {
        var root = CreateDirectory();
        var file = CreateSessionFile(root, "events.jsonl", "original");
        using var monitor = CreateMonitor(
            root,
            DateTimeOffset.UtcNow,
            "copilot",
            true,
            enableWatcher: true
        );
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.ActivityChanged += (_, _) => changed.TrySetResult();

        try
        {
            await File.AppendAllTextAsync(file, "-1", TestContext.Current.CancellationToken);
            await File.AppendAllTextAsync(file, "-2", TestContext.Current.CancellationToken);
            await File.AppendAllTextAsync(file, "-3", TestContext.Current.CancellationToken);
            await changed.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken
            );
            await Task.Delay(180, TestContext.Current.CancellationToken);

            monitor.DebouncedNotificationCount.Should().Be(1);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static CopilotActivityMonitor CreateMonitor(
        string root,
        DateTimeOffset now,
        string processName,
        bool live,
        bool enableWatcher)
    {

        var detector = new CopilotProcessHostDetector(
            processEnumerator: () =>
            [
                new CopilotProcessHostDetector.ProcessInfo
                {
                    Name = processName,
                    Pid = 42,
                    StartTimeUtc = now.AddHours(-1)
                }
            ],
            processLiveness: _ => live,
            extensionDirectories: []
        );

        return new CopilotActivityMonitor(
            root,
            detector,
            new FixedTimeProvider(now),
            enableWatcher: enableWatcher
        );
    }

    private static string CreateSessionFile(string root, string name, string content)
    {

        var directory = Path.Combine(root, ".copilot", "session-state", "session-1");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content);

        return path;
    }

    private static string CreateDirectory()
    {

        var directory = Path.Combine(Path.GetTempPath(), $"copilot-activity-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        return directory;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
}
