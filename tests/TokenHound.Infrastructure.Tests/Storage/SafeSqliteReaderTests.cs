using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Storage;

/// <summary>
/// Verifies concurrent WAL reading, fallback to immutable mode, and query capabilities in <see cref="SafeSqliteReader"/>.
/// </summary>
public sealed class SafeSqliteReaderTests
{
    /// <summary>
    /// Verifies reading committed data concurrently while an external writer holds an uncommitted transaction in WAL mode.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithConcurrentUncommittedWriter_ReadsCommittedDataWithoutLock()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            using var writer = new SqliteConnection($"Data Source={dbPath};Pooling=False");
            writer.Open();

            using var tx = writer.BeginTransaction();
            using var writeCmd = writer.CreateCommand();
            writeCmd.Transaction = tx;
            writeCmd.CommandText = "INSERT INTO Items (Val) VALUES ('UncommittedPending');";
            writeCmd.ExecuteNonQuery();

            var rows = await SafeSqliteReader.QueryAsync(
                dbPath,
                "SELECT Val FROM Items ORDER BY Id;",
                static r => r.GetString(0),
                cancellationToken: TestContext.Current.CancellationToken
            );

            rows.Should().Equal(["Alpha", "Beta"]);
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that ExecuteScalarAsync falls back to immutable mode when the -shm sidecar is absent.
    /// </summary>
    [Fact]
    public async Task ExecuteScalarAsync_WhenShmSidecarIsAbsent_FallsBackToImmutableAndReadsData()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            SqliteConnection.ClearAllPools();

            if (File.Exists(shmPath))
                File.Delete(shmPath);

            File.Exists(shmPath).Should().BeFalse();

            var count = await SafeSqliteReader.ExecuteScalarAsync<long>(
                dbPath,
                "SELECT COUNT(*) FROM Items;",
                cancellationToken: TestContext.Current.CancellationToken
            );

            count.Should().Be(2L);
            File.Exists(shmPath).Should().BeFalse();
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that QueryAsync correctly binds parameters and returns filtered results.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithParameters_ReturnsFilteredRows()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            var parameters = new Dictionary<string, object?> { ["val"] = "Alpha" };

            var rows = await SafeSqliteReader.QueryAsync(
                dbPath,
                "SELECT Val FROM Items WHERE Val = @val;",
                static r => r.GetString(0),
                parameters,
                TestContext.Current.CancellationToken
            );

            rows.Should().Equal(["Alpha"]);
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that ExecuteScalarAsync returns default when the query produces no rows or null.
    /// </summary>
    [Fact]
    public async Task ExecuteScalarAsync_WhenResultIsNull_ReturnsDefault()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            var result = await SafeSqliteReader.ExecuteScalarAsync<string>(
                dbPath,
                "SELECT Val FROM Items WHERE Id = 999;",
                cancellationToken: TestContext.Current.CancellationToken
            );

            result.Should().BeNull();
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that OpenReadOnly synchronously opens and returns an open connection.
    /// </summary>
    [Fact]
    public void OpenReadOnly_WhenCalledSynchronously_ReturnsOpenConnection()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            using var connection = SafeSqliteReader.OpenReadOnly(dbPath);

            connection.State.Should().Be(ConnectionState.Open);
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that OpenReadOnlyConnectionAsync throws FileNotFoundException when database file is missing.
    /// </summary>
    [Fact]
    public async Task OpenReadOnlyConnectionAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.db");

        var act = async () => await SafeSqliteReader.OpenReadOnlyConnectionAsync(
            missingPath,
            TestContext.Current.CancellationToken
        );

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    /// <summary>
    /// Verifies that OpenReadOnly throws ArgumentException when given null or whitespace path.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OpenReadOnly_WhenPathIsNullOrWhitespace_ThrowsArgumentException(string? invalidPath)
    {
        var act = () => SafeSqliteReader.OpenReadOnly(invalidPath!);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that OpenReadOnlyConnectionAsync observes cancellation tokens.
    /// </summary>
    [Fact]
    public async Task OpenReadOnlyConnectionAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var dbPath = CreateTestWalDatabase(out var shmPath, out var walPath);

        try
        {

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await SafeSqliteReader.OpenReadOnlyConnectionAsync(dbPath, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {

            CleanupDatabase(dbPath, shmPath, walPath);
        }
    }

    /// <summary>
    /// Verifies that BuildConnectionString and BuildFallbackConnectionString format correct options.
    /// </summary>
    [Fact]
    public void ConnectionStringBuilders_ProduceExpectedKeywords()
    {
        const string samplePath = @"C:\Users\Admin\test.db";
        var primary = SafeSqliteReader.BuildConnectionString(samplePath);
        var fallback = SafeSqliteReader.BuildFallbackConnectionString(samplePath);

        primary.Should().Contain("Mode=ReadOnly").And.Contain("Cache=Shared").And.Contain("Default Timeout=2");
        fallback.Should().Contain("immutable=1").And.Contain("Mode=ReadOnly");
    }

    private static string CreateTestWalDatabase(out string shmPath, out string walPath)
    {

        var path = Path.Combine(Path.GetTempPath(), $"safe_sqlite_test_{Guid.NewGuid():N}.db");
        shmPath = $"{path}-shm";
        walPath = $"{path}-wal";

        using var conn = new SqliteConnection($"Data Source={path};Pooling=False");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE Items (Id INTEGER PRIMARY KEY, Val TEXT); INSERT INTO Items (Val) VALUES ('Alpha'), ('Beta');";
        cmd.ExecuteNonQuery();

        return path;
    }

    private static void CleanupDatabase(string path, string shmPath, string walPath)
    {

        SqliteConnection.ClearAllPools();

        if (File.Exists(path))
            File.Delete(path);

        if (File.Exists(shmPath))
            File.Delete(shmPath);

        if (File.Exists(walPath))
            File.Delete(walPath);
    }
}
