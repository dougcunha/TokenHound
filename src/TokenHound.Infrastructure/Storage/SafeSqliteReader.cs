using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Storage;

/// <summary>
/// Provides safe, non-locking read-only access to SQLite databases in WAL mode
/// with automatic fallback to immutable mode when sidecars are absent or locked.
/// </summary>
public static class SafeSqliteReader
{
    private const string PROBE_QUERY = "SELECT 1;";
    private const int SQLITE_BUSY = 5;
    private const int SQLITE_LOCKED = 6;
    private const int SQLITE_CANTOPEN = 14;

    /// <summary>
    /// Builds the primary connection string for read-only WAL mode access.
    /// </summary>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <returns>The formatted connection string.</returns>
    public static string BuildConnectionString(string path)
        => $"Data Source={path};Mode=ReadOnly;Cache=Shared;Default Timeout=2;Pooling=False";

    /// <summary>
    /// Builds the fallback connection string for immutable read-only access.
    /// </summary>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <returns>The formatted fallback connection string.</returns>
    public static string BuildFallbackConnectionString(string path)
        => $"Data Source=file:{path}?immutable=1;Mode=ReadOnly;Cache=Shared;Default Timeout=2;Pooling=False";

    /// <summary>
    /// Synchronously opens a read-only SQLite connection with WAL mode and immutable fallback.
    /// </summary>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <returns>An open <see cref="SqliteConnection"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the database file does not exist.</exception>
    public static SqliteConnection OpenReadOnly(string path)
    {

        ValidateDatabaseFile(path);

        if (!HasShmSidecar(path))
            return CreateAndOpen(BuildFallbackConnectionString(path));

        try
        {

            var connection = CreateAndOpen(BuildConnectionString(path));
            using var probe = connection.CreateCommand();
            probe.CommandText = PROBE_QUERY;
            probe.ExecuteScalar();

            return connection;
        }
        catch (SqliteException ex) when (IsLockOrCantOpen(ex))
        {

            return CreateAndOpen(BuildFallbackConnectionString(path));
        }
    }

    /// <summary>
    /// Asynchronously opens a read-only SQLite connection with WAL mode and immutable fallback.
    /// </summary>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An open <see cref="SqliteConnection"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the database file does not exist.</exception>
    public static async Task<SqliteConnection> OpenReadOnlyConnectionAsync(
        string path,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        ValidateDatabaseFile(path);

        if (!HasShmSidecar(path))
            return await CreateAndOpenAsync(BuildFallbackConnectionString(path), cancellationToken).ConfigureAwait(false);

        try
        {

            var connection = await CreateAndOpenAsync(BuildConnectionString(path), cancellationToken).ConfigureAwait(false);
            using var probe = connection.CreateCommand();
            probe.CommandText = PROBE_QUERY;
            await probe.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return connection;
        }
        catch (SqliteException ex) when (IsLockOrCantOpen(ex))
        {

            return await CreateAndOpenAsync(BuildFallbackConnectionString(path), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a query and returns the first column converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The return type to convert the scalar result to.</typeparam>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <param name="commandText">The SQL query command text.</param>
    /// <param name="parameters">Optional query parameter key-value pairs.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The converted scalar result, or <see langword="default"/> if null or empty.</returns>
    public static async Task<T?> ExecuteScalarAsync<T>(
        string path,
        string commandText,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

        using var connection = await OpenReadOnlyConnectionAsync(path, cancellationToken).ConfigureAwait(false);
        using var command = CreateCommand(connection, commandText, parameters);
        var raw = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return ConvertScalar<T>(raw);
    }

    /// <summary>
    /// Executes a query and maps each row to <typeparamref name="T"/> using the provided mapper.
    /// </summary>
    /// <typeparam name="T">The mapped entity type.</typeparam>
    /// <param name="path">The path to the SQLite database file.</param>
    /// <param name="commandText">The SQL query command text.</param>
    /// <param name="mapRow">A delegate mapping each row from <see cref="SqliteDataReader"/> to <typeparamref name="T"/>.</param>
    /// <param name="parameters">Optional query parameter key-value pairs.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A read-only list of mapped items.</returns>
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
        string path,
        string commandText,
        Func<SqliteDataReader, T> mapRow,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        ArgumentNullException.ThrowIfNull(mapRow);

        using var connection = await OpenReadOnlyConnectionAsync(path, cancellationToken).ConfigureAwait(false);

        return await ExecuteReaderCoreAsync(
            connection,
            commandText,
            mapRow,
            parameters,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static SqliteConnection CreateAndOpen(string connectionString)
    {

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        return connection;
    }

    private static async Task<SqliteConnection> CreateAndOpenAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {

        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    private static async Task<IReadOnlyList<T>> ExecuteReaderCoreAsync<T>(
        SqliteConnection connection,
        string commandText,
        Func<SqliteDataReader, T> mapRow,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {

        using var command = CreateCommand(connection, commandText, parameters);
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var list = new List<T>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {

            list.Add(mapRow(reader));
        }

        return list;
    }

    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        string commandText,
        IReadOnlyDictionary<string, object?>? parameters)
    {

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        if (parameters is null)
            return command;

        foreach (var (key, val) in parameters)
        {

            var paramName = key.StartsWith('@') || key.StartsWith('$') || key.StartsWith(':')
                ? key
                : $"@{key}";

            command.Parameters.AddWithValue(paramName, val ?? DBNull.Value);
        }

        return command;
    }

    private static T? ConvertScalar<T>(object? raw)
    {

        if (raw is null || raw is DBNull)
            return default;

        return raw is T typed
            ? typed
            : (T?)Convert.ChangeType(raw, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
    }

    private static void ValidateDatabaseFile(string path)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("SQLite database file not found.", path);
    }

    private static bool HasShmSidecar(string path)
        => File.Exists($"{path}-shm");

    private static bool IsLockOrCantOpen(SqliteException ex)
        => (ex.SqliteErrorCode & 0xFF) is SQLITE_BUSY or SQLITE_LOCKED or SQLITE_CANTOPEN
            || ex.SqliteErrorCode is SQLITE_BUSY or SQLITE_LOCKED or SQLITE_CANTOPEN;
}
