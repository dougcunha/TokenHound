using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>Monitors Codex rollout and desktop database write activity.</summary>
public sealed class CodexActivityMonitor : IActivityMonitor
{
    /// <summary>The unique provider identifier for Codex.</summary>
    public const string PROVIDER_ID = "codex";

    private const string CODEX_DIRECTORY_NAME = ".codex";
    private const string DATABASE_FILE_NAME = "state_5.sqlite";
    private const string DESKTOP_DATABASE_FILE_NAME = "codex-dev.db";
    private const string ACTIVE_ROLLOUTS_QUERY = "SELECT rollout_path FROM threads WHERE archived = 0 ORDER BY updated_at_ms DESC LIMIT 8;";
    private const string ROLLOUT_SEARCH_PATTERN = "rollout-*.jsonl";
    private static readonly TimeSpan DEFAULT_BUSY_THRESHOLD = TimeSpan.FromSeconds(8);

    private readonly string _baseDirectory;
    private readonly TimeSpan _busyThreshold;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a monitor using the current user's profile and system time.</summary>
    /// <param name="baseDirectory">The user profile directory, or null for the current profile.</param>
    /// <param name="busyThreshold">The maximum age of a write that indicates activity.</param>
    /// <param name="timeProvider">The clock used to evaluate write age.</param>
    public CodexActivityMonitor(
        string? baseDirectory = null,
        TimeSpan? busyThreshold = null,
        TimeProvider? timeProvider = null)
    {

        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;
        _busyThreshold = busyThreshold ?? DEFAULT_BUSY_THRESHOLD;

        if (_busyThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(busyThreshold));

        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>Gets the user profile directory used for Codex discovery.</summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>Gets the desktop Codex database path used for activity detection.</summary>
    public string DesktopDatabasePath
        => Path.Combine(_baseDirectory, CODEX_DIRECTORY_NAME, "sqlite", DESKTOP_DATABASE_FILE_NAME);

    /// <inheritdoc />
    public async ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var rolloutPaths = await GetRolloutPathsAsync(cancellationToken).ConfigureAwait(false);
        var lastActivityUtc = FindLastActivityUtc(rolloutPaths);
        var desktopDatabaseActivityUtc = GetLastWriteTimeUtc(DesktopDatabasePath);

        if (desktopDatabaseActivityUtc is not null &&
            (lastActivityUtc is null || desktopDatabaseActivityUtc > lastActivityUtc))
            lastActivityUtc = desktopDatabaseActivityUtc;

        if (lastActivityUtc is null)
            return null;

        return CreateSession(lastActivityUtc.Value, _timeProvider.GetUtcNow());
    }

    private async Task<IReadOnlyList<string>> GetRolloutPathsAsync(CancellationToken cancellationToken)
    {

        var indexedPaths = await ReadActiveRolloutPathsAsync(cancellationToken).ConfigureAwait(false);

        return indexedPaths.Count > 0
            ? indexedPaths
            : FindRolloutFiles();
    }

    private async Task<IReadOnlyList<string>> ReadActiveRolloutPathsAsync(CancellationToken cancellationToken)
    {

        try
        {

            return await SafeSqliteReader.QueryAsync(
                Path.Combine(_baseDirectory, CODEX_DIRECTORY_NAME, DATABASE_FILE_NAME),
                ACTIVE_ROLLOUTS_QUERY,
                static reader => reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or SqliteException)
        {

            return [];
        }
    }

    private IReadOnlyList<string> FindRolloutFiles()
    {

        var codexDirectory = Path.Combine(_baseDirectory, CODEX_DIRECTORY_NAME);

        if (!Directory.Exists(codexDirectory))
            return [];

        try
        {

            var files = new List<string>();

            foreach (var file in Directory.EnumerateFiles(
                codexDirectory,
                ROLLOUT_SEARCH_PATTERN,
                SearchOption.AllDirectories))
                files.Add(file);

            return files;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return [];
        }
    }

    private DateTimeOffset? FindLastActivityUtc(IReadOnlyList<string> paths)
    {

        DateTimeOffset? latest = null;

        foreach (var path in paths)
        {

            var lastWrite = GetLastWriteTimeUtc(ResolvePath(path));

            if (lastWrite is not null && (latest is null || lastWrite > latest))
                latest = lastWrite;
        }

        return latest;
    }

    private string ResolvePath(string path)
        => Path.IsPathRooted(path)
            ? path
            : Path.Combine(_baseDirectory, path);

    private AgentSession CreateSession(DateTimeOffset lastActivityUtc, DateTimeOffset nowUtc)
        => new()
        {
            Pid = 0,
            StartTimeUtc = lastActivityUtc,
            State = IsBusy(lastActivityUtc, nowUtc) ? AgentSessionState.Busy : AgentSessionState.Idle,
            LastActivityUtc = lastActivityUtc
        };

    private bool IsBusy(DateTimeOffset lastActivityUtc, DateTimeOffset nowUtc)
    {

        var age = nowUtc - lastActivityUtc;

        return age <= _busyThreshold;
    }

    private static DateTimeOffset? GetLastWriteTimeUtc(string path)
    {

        try
        {

            if (!File.Exists(path))
                return null;

            var lastWriteUtc = File.GetLastWriteTimeUtc(path);

            return lastWriteUtc == DateTime.MinValue
                ? null
                : new DateTimeOffset(lastWriteUtc, TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return null;
        }
    }
}
