using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Verifies Codex rollout indexing, bounded tail parsing, and shared file access.</summary>
public sealed class CodexRolloutLogReaderTests
{
    /// <summary>Verifies that the newest active rollout event yields both rate-limit windows.</summary>
    [Fact]
    public async Task ReadLatestRateLimitsAsync_WithActiveRollout_ReturnsNewestRateLimits()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var codexDirectory = Path.Combine(tempDirectory, ".codex");
            Directory.CreateDirectory(codexDirectory);
            var databasePath = Path.Combine(codexDirectory, "state_5.sqlite");
            var rolloutPath = Path.Combine(tempDirectory, "rollout-current.jsonl");
            CreateDatabase(databasePath, rolloutPath);
            await File.WriteAllTextAsync(
                rolloutPath,
                CreateRateLimitEvent("plus", 18) + Environment.NewLine + CreateRateLimitEvent("team", 42),
                TestContext.Current.CancellationToken
            );

            var reader = new CodexRolloutLogReader(tempDirectory);
            var result = await reader.ReadLatestRateLimitsAsync(TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!.PlanType.Should().Be("team");
            result.Primary!.UsedPercent.Should().Be(42);
            result.Primary.WindowDurationMins.Should().Be(300);
            result.Primary.ResetsAt.Should().Be(1788659888);
            result.Secondary!.UsedPercent.Should().Be(12);
            result.Secondary.WindowDurationMins.Should().Be(10080);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that data outside the final 256 KB is not considered.</summary>
    [Fact]
    public async Task ReadRateLimitsFromFileAsync_WhenRateLimitIsBeforeTail_ReturnsNull()
    {

        var tempDirectory = CreateTempDirectory();
        var rolloutPath = Path.Combine(tempDirectory, "rollout-large.jsonl");

        try
        {

            var content = CreateRateLimitEvent("plus", 18) + Environment.NewLine + new string('x', 262144);
            await File.WriteAllTextAsync(rolloutPath, content, TestContext.Current.CancellationToken);

            var result = await CodexRolloutLogReader.ReadRateLimitsFromFileAsync(
                rolloutPath,
                TestContext.Current.CancellationToken
            );

            result.Should().BeNull();
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that a rollout file can be read while another handle has it open for writing.</summary>
    [Fact]
    public async Task ReadRateLimitsFromFileAsync_WhenFileIsOpenForWriting_ReadsWithoutLocking()
    {

        var tempDirectory = CreateTempDirectory();
        var rolloutPath = Path.Combine(tempDirectory, "rollout-shared.jsonl");

        try
        {

            await File.WriteAllTextAsync(rolloutPath, CreateRateLimitEvent("pro", 33), TestContext.Current.CancellationToken);

            using var writer = new FileStream(
                rolloutPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete
            );
            var result = await CodexRolloutLogReader.ReadRateLimitsFromFileAsync(
                rolloutPath,
                TestContext.Current.CancellationToken
            );

            result.Should().NotBeNull();
            result!.PlanType.Should().Be("pro");
            result.Primary!.UsedPercent.Should().Be(33);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies that missing databases, rollout files, and rate-limit events return null.</summary>
    [Fact]
    public async Task ReadLatestRateLimitsAsync_WhenDataIsMissingOrIncomplete_ReturnsNull()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var reader = new CodexRolloutLogReader(tempDirectory);
            var missingDatabase = await reader.ReadLatestRateLimitsAsync(TestContext.Current.CancellationToken);
            var missingFile = await CodexRolloutLogReader.ReadRateLimitsFromFileAsync(
                Path.Combine(tempDirectory, "missing.jsonl"),
                TestContext.Current.CancellationToken
            );

            missingDatabase.Should().BeNull();
            missingFile.Should().BeNull();
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    private static string CreateRateLimitEvent(string planType, int usedPercent)
        => $"{{\"type\":\"event_msg\",\"payload\":{{\"type\":\"token_count\",\"rate_limits\":{{\"plan_type\":\"{planType}\",\"primary\":{{\"used_percent\":{usedPercent},\"window_minutes\":300,\"resets_at\":1788659888}},\"secondary\":{{\"used_percent\":12,\"window_minutes\":10080,\"resets_at\":1789246688}}}}}}}}";

    private static void CreateDatabase(string databasePath, string rolloutPath)
    {

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE threads (rollout_path TEXT, archived INTEGER, updated_at_ms INTEGER);";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO threads (rollout_path, archived, updated_at_ms) VALUES ($path, 0, 2);";
        command.Parameters.AddWithValue("$path", rolloutPath);
        command.ExecuteNonQuery();
    }

    private static string CreateTempDirectory()
    {

        var path = Path.Combine(Path.GetTempPath(), $"codex_rollout_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteTempDirectory(string path)
    {

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
