using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers.Codex;

/// <summary>Verifies Codex activity detection from rollout and desktop database writes.</summary>
public sealed class CodexActivityMonitorTests
{
    /// <summary>Verifies that a recent indexed rollout write reports busy.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithRecentRolloutWrite_ReturnsBusy()
    {

        var tempDirectory = CreateTempDirectory();
        var now = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

        try
        {

            var rolloutPath = CreateIndexedRollout(tempDirectory);
            File.SetLastWriteTimeUtc(rolloutPath, now.AddSeconds(-3).UtcDateTime);

            var monitor = CreateMonitor(tempDirectory, now);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(AgentSessionState.Busy);
            session.LastActivityUtc.Should().Be(now.AddSeconds(-3));
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that a rollout write older than eight seconds reports idle.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithOldRolloutWrite_ReturnsIdle()
    {

        var tempDirectory = CreateTempDirectory();
        var now = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

        try
        {

            var rolloutPath = CreateIndexedRollout(tempDirectory);
            File.SetLastWriteTimeUtc(rolloutPath, now.AddSeconds(-9).UtcDateTime);

            var monitor = CreateMonitor(tempDirectory, now);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(AgentSessionState.Idle);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that a recent desktop database write reports busy.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithRecentDesktopDatabaseWrite_ReturnsBusy()
    {

        var tempDirectory = CreateTempDirectory();
        var now = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

        try
        {

            var databasePath = Path.Combine(tempDirectory, ".codex", "sqlite", "codex-dev.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            await File.WriteAllTextAsync(databasePath, "database", TestContext.Current.CancellationToken);
            File.SetLastWriteTimeUtc(databasePath, now.AddSeconds(-2).UtcDateTime);

            var monitor = CreateMonitor(tempDirectory, now);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().NotBeNull();
            session!.State.Should().Be(AgentSessionState.Busy);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that missing Codex directories return null without throwing.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WhenCodexFilesAreMissing_ReturnsNull()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var monitor = CreateMonitor(tempDirectory, DateTimeOffset.UtcNow);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

            session.Should().BeNull();
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies the provider identifier required by the activity contract.</summary>
    [Fact]
    public void ProviderId_ReturnsCodex()
    {

        var monitor = new CodexActivityMonitor();

        monitor.ProviderId.Should().Be("codex");
    }

    private static CodexActivityMonitor CreateMonitor(string baseDirectory, DateTimeOffset now)
        => new(baseDirectory, timeProvider: new FixedTimeProvider(now));

    private static string CreateIndexedRollout(string baseDirectory)
    {

        var codexDirectory = Path.Combine(baseDirectory, ".codex");
        Directory.CreateDirectory(codexDirectory);
        var databasePath = Path.Combine(codexDirectory, "state_5.sqlite");
        var rolloutPath = Path.Combine(codexDirectory, "sessions", "rollout-current.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(rolloutPath)!);
        File.WriteAllText(rolloutPath, "{}");

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE threads (rollout_path TEXT, archived INTEGER, updated_at_ms INTEGER);";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO threads (rollout_path, archived, updated_at_ms) VALUES ($path, 0, 1);";
        command.Parameters.AddWithValue("$path", rolloutPath);
        command.ExecuteNonQuery();

        return rolloutPath;
    }

    private static string CreateTempDirectory()
    {

        var path = Path.Combine(Path.GetTempPath(), $"codex_activity_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteTempDirectory(string path)
    {

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
}
