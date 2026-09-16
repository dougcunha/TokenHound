using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Reads Cline hub liveness and the latest session activity without writing to the Cline data directory.
/// </summary>
public sealed class ClineActivityReader
{
    /// <summary>The file name of the Cline hub lock file.</summary>
    public const string HUB_LOCK_FILE_NAME = "production.json";

    private const string CLINE_CONFIG_DIRECTORY = ".cline";
    private const string DATA_DIRECTORY = "data";
    private const string HUB_LOCKS_DIRECTORY = "locks/hub";
    private const string LAST_ACTIVITY_QUERY = "SELECT MAX(updated_at) FROM sessions;";
    private const string PID_PROPERTY = "pid";
    private const string STARTED_AT_PROPERTY = "startedAt";
    private const string SESSIONS_DATABASE_FILE_NAME = "sessions.db";
    private const string DATABASE_DIRECTORY = "db";

    private readonly string _hubLockFilePath;
    private readonly string _sessionsDatabasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineActivityReader"/> class.
    /// </summary>
    /// <param name="hubLockFilePath">An optional hub lock file path for testing.</param>
    /// <param name="sessionsDatabasePath">An optional sessions database path for testing.</param>
    public ClineActivityReader(
        string? hubLockFilePath = null,
        string? sessionsDatabasePath = null)
    {

        _hubLockFilePath = string.IsNullOrWhiteSpace(hubLockFilePath)
            ? GetDefaultHubLockFilePath()
            : hubLockFilePath;
        _sessionsDatabasePath = string.IsNullOrWhiteSpace(sessionsDatabasePath)
            ? GetDefaultSessionsDatabasePath()
            : sessionsDatabasePath;
    }

    /// <summary>
    /// Gets the resolved Cline hub lock file path.
    /// </summary>
    public string HubLockFilePath
        => _hubLockFilePath;

    /// <summary>
    /// Gets the resolved Cline sessions database path.
    /// </summary>
    public string SessionsDatabasePath
        => _sessionsDatabasePath;

    /// <summary>
    /// Reads the hub daemon advertised by the Cline lock file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The hub snapshot, or <see langword="null"/> when no usable lock file exists.</returns>
    public async Task<ClineHubSnapshot?> ReadHubAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        try
        {

            var jsonContent = await SharedFileReader.ReadAllTextAsync(_hubLockFilePath, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(jsonContent))
                return null;

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(PID_PROPERTY, out var pidElement) ||
                !pidElement.TryGetInt32(out var pid) ||
                pid <= 0)
                return null;

            return new ClineHubSnapshot
            {
                Pid = pid,
                StartedAtUtc = ReadStartedAtUtc(root)
            };
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            return null;
        }
    }

    /// <summary>
    /// Reads the most recent session update recorded by the Cline CLI.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The latest activity timestamp in UTC, or null when no usable reading exists.</returns>
    public async Task<DateTimeOffset?> ReadLastActivityUtcAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        try
        {

            var rows = await SafeSqliteReader.QueryAsync<string?>(
                _sessionsDatabasePath,
                LAST_ACTIVITY_QUERY,
                static reader => reader.IsDBNull(0) ? null : reader.GetString(0),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var timestamp = rows.Count == 0 ? null : rows[0];

            return DateTimeOffset.TryParse(timestamp, out var parsed) ? parsed.ToUniversalTime() : null;
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

    /// <summary>
    /// Gets the default Cline hub lock file path under the current user's profile.
    /// </summary>
    /// <returns>The fully qualified default path to the hub lock file.</returns>
    public static string GetDefaultHubLockFilePath()
        => Path.Combine(
            GetDefaultDataDirectory(),
            HUB_LOCKS_DIRECTORY,
            HUB_LOCK_FILE_NAME
        );

    /// <summary>
    /// Gets the default Cline sessions database path under the current user's profile.
    /// </summary>
    /// <returns>The fully qualified default path to the sessions database.</returns>
    public static string GetDefaultSessionsDatabasePath()
        => Path.Combine(
            GetDefaultDataDirectory(),
            DATABASE_DIRECTORY,
            SESSIONS_DATABASE_FILE_NAME
        );

    private static DateTimeOffset ReadStartedAtUtc(JsonElement root)
    {

        if (root.TryGetProperty(STARTED_AT_PROPERTY, out var startedAt) &&
            startedAt.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(startedAt.GetString(), out var parsed))
            return parsed.ToUniversalTime();

        return DateTimeOffset.MinValue;
    }

    private static string GetDefaultDataDirectory()
    {

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(userProfile))
            userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? ".";

        return Path.Combine(userProfile, CLINE_CONFIG_DIRECTORY, DATA_DIRECTORY);
    }
}