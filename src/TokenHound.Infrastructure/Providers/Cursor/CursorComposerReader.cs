using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Reads and parses Composer conversation headers from Cursor's SQLite database.
/// </summary>
public sealed class CursorComposerReader
{
    private const string SQL = "SELECT value FROM composerHeaders WHERE isArchived = 0 ORDER BY recency DESC LIMIT 40;";

    private readonly string? _customDatabasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorComposerReader"/> class.
    /// </summary>
    /// <param name="customDatabasePath">Optional path to state.vscdb for testing.</param>
    public CursorComposerReader(string? customDatabasePath = null)
    {
        _customDatabasePath = customDatabasePath;
    }

    /// <summary>
    /// Reads active unarchived Composer headers from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of parsed composer headers.</returns>
    public async Task<IReadOnlyList<CursorComposerHeaderDto>> ReadActiveHeadersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dbPath = ResolveDatabasePath();

        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            return [];

        try
        {

            var rows = await SafeSqliteReader.QueryAsync(
                dbPath,
                SQL,
                static reader => reader.IsDBNull(0) ? null : reader.GetString(0),
                parameters: null,
                cancellationToken).ConfigureAwait(false);

            return ParseHeaders(rows);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IReadOnlyList<CursorComposerHeaderDto> ParseHeaders(IReadOnlyList<string?> rows)
    {

        var result = new List<CursorComposerHeaderDto>();

        foreach (var json in rows)
        {

            if (string.IsNullOrWhiteSpace(json))
                continue;

            try
            {

                var header = JsonSerializer.Deserialize<CursorComposerHeaderDto>(json);

                if (header is not null)
                {
                    result.Add(header);
                }
            }
            catch (JsonException)
            {
                // Skip malformed individual rows
            }
        }

        return result;
    }

    private string? ResolveDatabasePath()
    {

        if (!string.IsNullOrEmpty(_customDatabasePath))
            return _customDatabasePath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return Path.Combine(appData, "Cursor", "User", "globalStorage", "state.vscdb");
    }
}
