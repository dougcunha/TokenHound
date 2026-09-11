using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.OpenCode;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.OpenCode;

/// <summary>Verifies OpenCode process validation and recent SQLite activity detection.</summary>
public sealed class OpenCodeActivityMonitorTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FIXED_START = FIXED_NOW.AddMinutes(-30);

    /// <summary>Verifies that ProviderId returns "opencode".</summary>
    [Fact]
    public void ProviderId_ReturnsOpenCode()
    {

        var monitor = new OpenCodeActivityMonitor();

        monitor.ProviderId.Should().Be("opencode");
    }

    /// <summary>Verifies that both supported process names are monitored.</summary>
    [Fact]
    public void MonitoredProcessNames_ContainsOpencodeAndOpenCode()
    {

        OpenCodeActivityMonitor.MONITORED_PROCESS_NAMES.Should().Contain("opencode");
        OpenCodeActivityMonitor.MONITORED_PROCESS_NAMES.Should().Contain("OpenCode");
    }

    /// <summary>Verifies that no live process produces a null session without reading the database.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenProcessIsAbsent_ReturnsNull()
    {

        var monitor = new OpenCodeActivityMonitor(
            processLocator: static () => null,
            activityReader: static _ => throw new InvalidOperationException("activity must not be read"),
            processLiveness: static (_, _) => true);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
    }

    /// <summary>Verifies that invalid process identifiers are ignored.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenProcessIdIsInvalid_ReturnsNull()
    {

        var monitor = CreateUnitMonitor(static () => (0, FIXED_START), FIXED_NOW);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
    }

    /// <summary>Verifies that a PID/start-time validation failure is not treated as a live process.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenProcessStartTimeValidationFails_ReturnsNull()
    {

        var monitor = new OpenCodeActivityMonitor(
            processLocator: static () => (4567, FIXED_START),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(FIXED_NOW),
            processLiveness: static (_, _) => false);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
    }

    /// <summary>Verifies that activity within the sixty-second threshold reports Busy.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenActivityIsRecent_ReturnsBusy()
    {

        var lastActivity = FIXED_NOW.AddSeconds(-60);
        var monitor = CreateUnitMonitor(static () => (4567, FIXED_START), lastActivity);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.Pid.Should().Be(4567);
        session.StartTimeUtc.Should().Be(FIXED_START);
        session.State.Should().Be(AgentSessionState.Busy);
        session.LastActivityUtc.Should().Be(lastActivity);
    }

    /// <summary>Verifies that stale activity for a live process reports Idle.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenActivityIsStale_ReturnsIdle()
    {

        var lastActivity = FIXED_NOW.AddSeconds(-61);
        var monitor = CreateUnitMonitor(static () => (4567, FIXED_START), lastActivity);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.State.Should().Be(AgentSessionState.Idle);
        session.LastActivityUtc.Should().Be(lastActivity);
    }

    /// <summary>Verifies that a live process without a database reading reports Idle.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenActivityIsUnavailable_ReturnsIdle()
    {

        var monitor = CreateUnitMonitor(
            static () => (4567, FIXED_START),
            activityUtc: null);

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.State.Should().Be(AgentSessionState.Idle);
        session.LastActivityUtc.Should().Be(FIXED_START);
    }

    /// <summary>Verifies that the CLI process name is discovered.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenCliProcessIsFound_ReturnsSession()
    {

        var monitor = CreateNameMonitor("opencode");
        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.Pid.Should().Be(1111);
    }

    /// <summary>Verifies that the Desktop process name is discovered.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenDesktopProcessIsFound_ReturnsSession()
    {

        var monitor = CreateNameMonitor("OpenCode");
        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.Pid.Should().Be(2222);
    }

    /// <summary>Verifies recent activity through SafeSqliteReader's primary WAL path.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithRecentWalActivity_ReturnsBusy()
    {

        var databasePath = CreateActivityDatabase(FIXED_NOW.AddSeconds(-10), out var shmPath, out var walPath);

        try
        {

            var monitor = CreateDatabaseMonitor(databasePath);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(AgentSessionState.Busy);
            session.LastActivityUtc.Should().Be(FIXED_NOW.AddSeconds(-10));
        }
        finally
        {

            CleanupDatabase(databasePath, shmPath, walPath);
        }
    }

    /// <summary>Verifies stale activity through SafeSqliteReader's immutable fallback.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithStaleActivityAndMissingShm_ReturnsIdle()
    {

        var databasePath = CreateActivityDatabase(FIXED_NOW.AddSeconds(-61), out var shmPath, out var walPath);

        try
        {

            SqliteConnection.ClearAllPools();

            if (File.Exists(shmPath))
                File.Delete(shmPath);

            var monitor = CreateDatabaseMonitor(databasePath);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(AgentSessionState.Idle);
            session.LastActivityUtc.Should().Be(FIXED_NOW.AddSeconds(-61));
        }
        finally
        {

            CleanupDatabase(databasePath, shmPath, walPath);
        }
    }

    /// <summary>Verifies that a requested cancellation is propagated.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        var monitor = new OpenCodeActivityMonitor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await monitor.CheckLivenessAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static OpenCodeActivityMonitor CreateUnitMonitor(
        Func<(int Pid, DateTimeOffset StartTimeUtc)?> processLocator,
        DateTimeOffset? activityUtc)
        => new(
            processLocator: processLocator,
            timeProvider: new FixedTimeProvider(FIXED_NOW),
            activityReader: _ => Task.FromResult(activityUtc),
            processLiveness: static (_, _) => true);

    private static OpenCodeActivityMonitor CreateNameMonitor(string processName)
        => new(
            processByNameFinder: name => string.Equals(name, processName, StringComparison.Ordinal)
                ? (processName == "opencode" ? 1111 : 2222, FIXED_START)
                : null,
            timeProvider: new FixedTimeProvider(FIXED_NOW),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(FIXED_NOW),
            processLiveness: static (_, _) => true);

    private static OpenCodeActivityMonitor CreateDatabaseMonitor(string databasePath)
        => new(
            processLocator: static () => (4567, FIXED_START),
            timeProvider: new FixedTimeProvider(FIXED_NOW),
            activityReader: new OpenCodeActivityReader(databasePath).ReadLastActivityUtcAsync,
            processLiveness: static (_, _) => true);

    private static string CreateActivityDatabase(
        DateTimeOffset activityUtc,
        out string shmPath,
        out string walPath)
    {

        var databasePath = Path.Combine(Path.GetTempPath(), $"opencode_activity_{Guid.NewGuid():N}.db");
        shmPath = $"{databasePath}-shm";
        walPath = $"{databasePath}-wal";

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE session (time_updated INTEGER); INSERT INTO session (time_updated) VALUES ($time);";
        command.Parameters.AddWithValue("$time", activityUtc.ToUnixTimeMilliseconds());
        command.ExecuteNonQuery();

        return databasePath;
    }

    private static void CleanupDatabase(string databasePath, string shmPath, string walPath)
    {

        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { databasePath, shmPath, walPath })
        {

            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
}
