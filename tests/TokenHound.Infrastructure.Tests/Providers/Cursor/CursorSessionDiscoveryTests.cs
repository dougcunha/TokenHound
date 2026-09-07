using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

/// <summary>
/// Unit tests for <see cref="CursorSessionDiscovery"/>.
/// </summary>
public sealed class CursorSessionDiscoveryTests
{
    [Fact]
    public async Task DiscoverAuthAsync_WithValidDatabase_ReturnsCursorAuthDto()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateStateDatabase(
            dbPath,
            accessToken: "test-jwt-access-token",
            membershipAuthId: "user_01JT4P1FS4AB8WA4N...",
            email: "developer@example.com",
            membershipType: "pro");

        try
        {
            var discovery = new CursorSessionDiscovery(dbPath);

            // Act
            var auth = await discovery.DiscoverAuthAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(auth);
            Assert.Equal("test-jwt-access-token", auth.AccessToken);
            Assert.Equal("user_01JT4P1FS4AB8WA4N...", auth.StripeMembershipAuthId);
            Assert.Equal("developer@example.com", auth.CachedEmail);
            Assert.Equal("pro", auth.StripeMembershipType);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAuthAsync_WhenDatabaseMissing_ReturnsNull()
    {
        // Arrange
        var discovery = new CursorSessionDiscovery("C:\\nonexistent_cursor_db_123.vscdb");

        // Act
        var auth = await discovery.DiscoverAuthAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(auth);
    }

    [Fact]
    public async Task DiscoverAuthAsync_WhenAccessTokenMissing_ReturnsNull()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_test_no_token_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateStateDatabase(
            dbPath,
            accessToken: null,
            membershipAuthId: "user_12345",
            email: "dev@example.com",
            membershipType: "free");

        try
        {
            var discovery = new CursorSessionDiscovery(dbPath);

            // Act
            var auth = await discovery.DiscoverAuthAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(auth);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAuthAsync_PropagatesCancellation()
    {
        // Arrange
        var discovery = new CursorSessionDiscovery();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await discovery.DiscoverAuthAsync(cts.Token);
        });
    }

    private static void CreateStateDatabase(
        string dbPath,
        string? accessToken,
        string? membershipAuthId,
        string? email,
        string? membershipType)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE ItemTable (key TEXT PRIMARY KEY, value TEXT);";
        cmd.ExecuteNonQuery();

        InsertKey(connection, "cursorAuth/accessToken", accessToken);
        InsertKey(connection, "cursorAuth/stripeMembershipAuthId", membershipAuthId);
        InsertKey(connection, "cursorAuth/cachedEmail", email);
        InsertKey(connection, "cursorAuth/stripeMembershipType", membershipType);
    }

    private static void InsertKey(SqliteConnection connection, string key, string? value)
    {
        if (value is null)
            return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO ItemTable (key, value) VALUES (@k, @v);";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.ExecuteNonQuery();
    }
}
