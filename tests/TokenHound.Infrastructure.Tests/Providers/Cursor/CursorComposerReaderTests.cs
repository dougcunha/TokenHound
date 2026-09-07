using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

/// <summary>
/// Unit tests for <see cref="CursorComposerReader"/>.
/// </summary>
public sealed class CursorComposerReaderTests
{
    [Fact]
    public async Task ReadActiveHeadersAsync_WithValidDatabase_ReturnsDeserializedHeaders()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_comp_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateComposerDatabase(dbPath);

        try
        {

            var reader = new CursorComposerReader(dbPath);

            // Act
            var headers = await reader.ReadActiveHeadersAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(headers);
            Assert.Equal(2, headers.Count);

            var active = headers[0];
            Assert.Equal("comp-1", active.ComposerId);
            Assert.Equal("Auth Refactor", active.Name);
            Assert.True(active.HasUnfinishedRun);
            Assert.False(active.IsWaitingApproval);
            Assert.NotNull(active.LatestCheckpointUtc);

            var waiting = headers[1];
            Assert.Equal("comp-2", waiting.ComposerId);
            Assert.False(waiting.HasUnfinishedRun);
            Assert.True(waiting.IsWaitingApproval);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadActiveHeadersAsync_WhenDatabaseMissing_ReturnsEmptyList()
    {
        // Arrange
        var reader = new CursorComposerReader("C:\\nonexistent_cursor_comp_db.vscdb");

        // Act
        var headers = await reader.ReadActiveHeadersAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(headers);
        Assert.Empty(headers);
    }

    [Fact]
    public async Task ReadActiveHeadersAsync_WhenTableMissing_ReturnsEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_empty_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE OtherTable (id INTEGER PRIMARY KEY);";
            cmd.ExecuteNonQuery();
        }

        try
        {

            var reader = new CursorComposerReader(dbPath);

            // Act
            var headers = await reader.ReadActiveHeadersAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(headers);
            Assert.Empty(headers);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadActiveHeadersAsync_WhenRowMalformed_SkipsMalformedRow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_malformed_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE composerHeaders (composerId TEXT PRIMARY KEY, isArchived INTEGER, recency INTEGER, value TEXT);";
            cmd.ExecuteNonQuery();

            InsertComposerHeader(connection, "bad-1", 0, 10, "{invalid json content}");
            InsertComposerHeader(connection, "good-1", 0, 20, "{\"composerId\":\"good-1\",\"name\":\"Valid Session\"}");
        }

        try
        {

            var reader = new CursorComposerReader(dbPath);

            // Act
            var headers = await reader.ReadActiveHeadersAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(headers);
            Assert.Single(headers);
            Assert.Equal("good-1", headers[0].ComposerId);
            Assert.Equal("Valid Session", headers[0].Name);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadActiveHeadersAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var reader = new CursorComposerReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await reader.ReadActiveHeadersAsync(cts.Token);
        });
    }

    private static void CreateComposerDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE composerHeaders (composerId TEXT PRIMARY KEY, isArchived INTEGER, recency INTEGER, value TEXT);";
        cmd.ExecuteNonQuery();

        const string JSON1 = """
            {
              "composerId": "comp-1",
              "name": "Auth Refactor",
              "subtitle": "Cursor Composer",
              "unfinishedRunAt": 1788645000000,
              "hasBlockingPendingActions": false,
              "hasPendingPlan": false,
              "conversationCheckpointLastUpdatedAt": 1788645120000,
              "lastUpdatedAt": 1788645120000,
              "createdAt": 1788644000000
            }
            """;

        const string JSON2 = """
            {
              "composerId": "comp-2",
              "name": "Plan Approval",
              "subtitle": "Cursor Composer",
              "unfinishedRunAt": null,
              "hasBlockingPendingActions": true,
              "hasPendingPlan": true,
              "conversationCheckpointLastUpdatedAt": 1788645100000,
              "lastUpdatedAt": 1788645100000,
              "createdAt": 1788644500000
            }
            """;

        const string JSON_ARCHIVED = """
            {
              "composerId": "comp-archived",
              "name": "Old Completed Session",
              "unfinishedRunAt": null
            }
            """;

        InsertComposerHeader(connection, "comp-1", 0, 100, JSON1);
        InsertComposerHeader(connection, "comp-2", 0, 50, JSON2);
        InsertComposerHeader(connection, "comp-archived", 1, 200, JSON_ARCHIVED);
    }

    private static void InsertComposerHeader(SqliteConnection connection, string id, int isArchived, long recency, string value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO composerHeaders (composerId, isArchived, recency, value) VALUES (@id, @arch, @rec, @val);";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@arch", isArchived);
        cmd.Parameters.AddWithValue("@rec", recency);
        cmd.Parameters.AddWithValue("@val", value);
        cmd.ExecuteNonQuery();
    }
}
