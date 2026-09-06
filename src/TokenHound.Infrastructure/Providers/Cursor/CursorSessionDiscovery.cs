using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Discovers Cursor authentication and account metadata from the local state.vscdb SQLite database.
/// </summary>
public sealed class CursorSessionDiscovery
{
    private const string KEY_ACCESS_TOKEN = "cursorAuth/accessToken";
    private const string KEY_MEMBERSHIP_AUTH_ID = "cursorAuth/stripeMembershipAuthId";
    private const string KEY_CACHED_EMAIL = "cursorAuth/cachedEmail";
    private const string KEY_MEMBERSHIP_TYPE = "cursorAuth/stripeMembershipType";

    private readonly string? _customDatabasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorSessionDiscovery"/> class.
    /// </summary>
    /// <param name="customDatabasePath">Optional path to state.vscdb for testing.</param>
    public CursorSessionDiscovery(string? customDatabasePath = null)
    {
        _customDatabasePath = customDatabasePath;
    }

    /// <summary>
    /// Discovers active Cursor authentication credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted auth credentials, or null if database or required keys are missing.</returns>
    public async ValueTask<CursorAuthDto?> DiscoverAuthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dbPath = ResolveDatabasePath();

        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
        {
            return null;
        }

        try
        {
            const string SQL = "SELECT key, value FROM ItemTable WHERE key IN (@k1, @k2, @k3, @k4);";

            var parameters = new Dictionary<string, object?>
            {
                ["@k1"] = KEY_ACCESS_TOKEN,
                ["@k2"] = KEY_MEMBERSHIP_AUTH_ID,
                ["@k3"] = KEY_CACHED_EMAIL,
                ["@k4"] = KEY_MEMBERSHIP_TYPE
            };

            var rows = await SafeSqliteReader.QueryAsync(
                dbPath,
                SQL,
                mapRow: reader => (Key: reader.GetString(0), Value: reader.IsDBNull(1) ? null : reader.GetString(1)),
                parameters: parameters,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            return ExtractAuthFromRows(rows);
        }
        catch
        {
            return null;
        }
    }

    private static CursorAuthDto? ExtractAuthFromRows(IReadOnlyList<(string Key, string? Value)> rows)
    {
        string? accessToken = null;
        string? membershipAuthId = null;
        string? cachedEmail = null;
        string? membershipType = null;

        foreach (var (key, value) in rows)
        {

            if (string.Equals(key, KEY_ACCESS_TOKEN, StringComparison.Ordinal))
            {
                accessToken = value;
            }
            else if (string.Equals(key, KEY_MEMBERSHIP_AUTH_ID, StringComparison.Ordinal))
            {
                membershipAuthId = value;
            }
            else if (string.Equals(key, KEY_CACHED_EMAIL, StringComparison.Ordinal))
            {
                cachedEmail = value;
            }
            else if (string.Equals(key, KEY_MEMBERSHIP_TYPE, StringComparison.Ordinal))
            {
                membershipType = value;
            }
        }

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(membershipAuthId))
        {
            return null;
        }

        return new CursorAuthDto
        {
            AccessToken = accessToken,
            StripeMembershipAuthId = membershipAuthId,
            CachedEmail = cachedEmail,
            StripeMembershipType = membershipType
        };
    }

    private string? ResolveDatabasePath()
    {

        if (!string.IsNullOrEmpty(_customDatabasePath))
        {
            return _customDatabasePath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return Path.Combine(appData, "Cursor", "User", "globalStorage", "state.vscdb");
    }
}
