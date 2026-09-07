using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

/// <summary>
/// Unit tests for <see cref="CursorActivityMonitor"/>.
/// </summary>
public sealed class CursorActivityMonitorTests
{
    [Fact]
    public async Task CheckLivenessAsync_WhenProcessNotRunning_ReturnsNull()
    {
        // Arrange
        var monitor = new CursorActivityMonitor(
            processLocator: static () => null);

        // Act
        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenNoHeaders_ReturnsIdleSession()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(-1);
        var monitor = new CursorActivityMonitor(
            composerReader: new CursorComposerReader("C:\\nonexistent_db.vscdb"),
            processLocator: () => (1234, startTime));

        // Act
        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(1234, session.Pid);
        Assert.Equal(AgentSessionState.Idle, session.State);
        Assert.Equal(startTime, session.StartTimeUtc);
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenActiveUnfinishedRun_ReturnsBusySession()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_mon_busy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        var now = DateTimeOffset.UtcNow;
        var processStart = now.AddMinutes(-30);
        var checkpointTime = now.AddMinutes(-2);
        var checkpointMs = checkpointTime.ToUnixTimeMilliseconds();

        CreateComposerDatabaseWithJson(dbPath, $$"""
            {
              "composerId": "active-1",
              "unfinishedRunAt": {{checkpointMs}},
              "conversationCheckpointLastUpdatedAt": {{checkpointMs}}
            }
            """);

        try
        {
            var reader = new CursorComposerReader(dbPath);
            var monitor = new CursorActivityMonitor(
                composerReader: reader,
                processLocator: () => (4321, processStart));

            // Act
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(4321, session.Pid);
            Assert.Equal(AgentSessionState.Busy, session.State);
            Assert.Equal(checkpointTime.ToUnixTimeSeconds(), session.LastActivityUtc.ToUnixTimeSeconds());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenRunIsStale_ReturnsIdleSession()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_mon_stale_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        var now = DateTimeOffset.UtcNow;
        var processStart = now.AddHours(-2);
        var checkpointTime = now.AddMinutes(-20); // older than 15 min threshold
        var checkpointMs = checkpointTime.ToUnixTimeMilliseconds();

        CreateComposerDatabaseWithJson(dbPath, $$"""
            {
              "composerId": "stale-1",
              "unfinishedRunAt": {{checkpointMs}},
              "conversationCheckpointLastUpdatedAt": {{checkpointMs}}
            }
            """);

        try
        {
            var reader = new CursorComposerReader(dbPath);
            var monitor = new CursorActivityMonitor(
                composerReader: reader,
                processLocator: () => (5555, processStart),
                staleThreshold: TimeSpan.FromMinutes(15));

            // Act
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(AgentSessionState.Idle, session.State);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenWaitingApproval_ReturnsWaitingSession()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_mon_waiting_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        var now = DateTimeOffset.UtcNow;
        var processStart = now.AddHours(-1);

        CreateComposerDatabaseWithJson(dbPath, """
            {
              "composerId": "waiting-1",
              "unfinishedRunAt": null,
              "hasBlockingPendingActions": true
            }
            """);

        try
        {
            var reader = new CursorComposerReader(dbPath);
            var monitor = new CursorActivityMonitor(
                composerReader: reader,
                processLocator: () => (7777, processStart));

            // Act
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(AgentSessionState.Waiting, session.State);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var monitor = new CursorActivityMonitor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await monitor.CheckLivenessAsync(cts.Token);
        });
    }

    private static void CreateComposerDatabaseWithJson(string dbPath, string json)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE composerHeaders (composerId TEXT PRIMARY KEY, isArchived INTEGER, recency INTEGER, value TEXT);";
        cmd.ExecuteNonQuery();

        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO composerHeaders (composerId, isArchived, recency, value) VALUES (@id, 0, 100, @val);";
        insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        insertCmd.Parameters.AddWithValue("@val", json);
        insertCmd.ExecuteNonQuery();
    }
}
