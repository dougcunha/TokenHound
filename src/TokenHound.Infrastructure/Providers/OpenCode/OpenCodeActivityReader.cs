using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>Reads the latest OpenCode session activity without writing to its SQLite database.</summary>
public sealed class OpenCodeActivityReader
{
    private const string DATABASE_DIRECTORY = ".local";
    private const string DATABASE_FILE_NAME = "opencode.db";
    private const string LAST_ACTIVITY_QUERY = "SELECT MAX(time_updated) FROM session;";

    private readonly string _databasePath;

    /// <summary>
    /// Initializes a reader for the standard OpenCode database or a supplied test path.
    /// </summary>
    /// <param name="databasePath">Optional path to an OpenCode database.</param>
    public OpenCodeActivityReader(string? databasePath = null)
    {

        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? GetDefaultDatabasePath()
            : databasePath;
    }

    /// <summary>Gets the database path used for read-only activity queries.</summary>
    public string DatabasePath
        => _databasePath;

    /// <summary>Reads the latest session update timestamp, or null when unavailable.</summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The latest activity timestamp in UTC, or null when no usable reading exists.</returns>
    public async Task<DateTimeOffset?> ReadLastActivityUtcAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        try
        {

            var rows = await SafeSqliteReader.QueryAsync<long?>(
                _databasePath,
                LAST_ACTIVITY_QUERY,
                static reader => reader.IsDBNull(0) ? null : reader.GetInt64(0),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var timestamp = rows.Count == 0 ? null : rows[0];

            return timestamp.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value)
                : null;
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or SqliteException or ArgumentOutOfRangeException)
        {

            return null;
        }
    }

    private static string GetDefaultDatabasePath()
    {

        var profileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Path.Combine(
            profileDirectory,
            DATABASE_DIRECTORY,
            "share",
            "opencode",
            DATABASE_FILE_NAME);
    }
}
