using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Discovers and parses Claude Code credentials from default and multi-profile directories.
/// </summary>
public sealed class ClaudeProfileDiscovery
{
    private const string CREDENTIALS_FILE_NAME = ".credentials.json";
    private const string DEFAULT_PROFILE_DIR = ".claude";
    private const string PROFILE_PATTERN = ".claude-*";
    private const string OAUTH_PROPERTY_NAME = "claudeAiOauth";
    private const string ACCESS_TOKEN_PROPERTY_NAME = "accessToken";
    private const string TOKEN_PROPERTY_NAME = "token";
    private const string EXPIRES_AT_PROPERTY_NAME = "expiresAt";
    private const string EXPIRES_AT_SNAKE_PROPERTY_NAME = "expires_at";

    private readonly string _baseDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeProfileDiscovery"/> class.
    /// </summary>
    /// <param name="baseDirectory">
    /// The base directory containing Claude profiles, or <see langword="null"/> to use the user profile.
    /// </param>
    public ClaudeProfileDiscovery(string? baseDirectory = null)
    {

        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;
    }

    /// <summary>
    /// Gets the base directory used for profile discovery.
    /// </summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>
    /// Discovers and reads the Claude credential from the default profile location.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The credential if found and valid; otherwise, <see langword="null"/>.</returns>
    public Task<ClaudeCredentialDto?> DiscoverDefaultCredentialAsync(CancellationToken cancellationToken = default)
    {

        var defaultPath = Path.Combine(_baseDirectory, DEFAULT_PROFILE_DIR, CREDENTIALS_FILE_NAME);

        return LoadCredentialFromFileAsync(defaultPath, cancellationToken);
    }

    /// <summary>
    /// Discovers the active Claude credential, checking the default profile first and then multi-profile directories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The first valid credential found; otherwise, <see langword="null"/>.</returns>
    public async Task<ClaudeCredentialDto?> DiscoverCredentialAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var defaultCredential = await DiscoverDefaultCredentialAsync(cancellationToken).ConfigureAwait(false);

        if (defaultCredential is not null)
            return defaultCredential;

        var multiCredentials = await DiscoverMultiProfileCredentialsAsync(cancellationToken).ConfigureAwait(false);

        return multiCredentials.FirstOrDefault();
    }

    /// <summary>
    /// Discovers all Claude credentials across the default profile and any multi-profile directories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A read-only list of discovered credentials.</returns>
    public async Task<IReadOnlyList<ClaudeCredentialDto>> DiscoverAllCredentialsAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<ClaudeCredentialDto>();

        var defaultCredential = await DiscoverDefaultCredentialAsync(cancellationToken).ConfigureAwait(false);

        if (defaultCredential is not null)
            results.Add(defaultCredential);

        var multiCredentials = await DiscoverMultiProfileCredentialsAsync(cancellationToken).ConfigureAwait(false);

        results.AddRange(multiCredentials);

        return results;
    }

    /// <summary>
    /// Reads and parses a Claude credential from the specified file path.
    /// </summary>
    /// <param name="filePath">The absolute path to the credentials JSON file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The parsed credential, or <see langword="null"/> if the file is missing or invalid.</returns>
    public static async Task<ClaudeCredentialDto?> LoadCredentialFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {

            var jsonContent = await SharedFileReader.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            return ParseCredentialJson(jsonContent);
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
    /// Parses a JSON string containing Claude Code credentials.
    /// </summary>
    /// <param name="jsonContent">The raw JSON string to parse.</param>
    /// <returns>The extracted <see cref="ClaudeCredentialDto"/>, or <see langword="null"/> if parsing fails or no token is found.</returns>
    public static ClaudeCredentialDto? ParseCredentialJson(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var token = ExtractToken(root);

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return new ClaudeCredentialDto { AccessToken = token, ExpiresAt = ExtractExpiresAt(root) };
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private async Task<IReadOnlyList<ClaudeCredentialDto>> DiscoverMultiProfileCredentialsAsync(CancellationToken cancellationToken)
    {

        if (!Directory.Exists(_baseDirectory))
            return [];

        var results = new List<ClaudeCredentialDto>();
        var matchingDirs = Directory.GetDirectories(_baseDirectory, PROFILE_PATTERN)
            .OrderBy(static dir => dir, StringComparer.OrdinalIgnoreCase);

        foreach (var dir in matchingDirs)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var credPath = Path.Combine(dir, CREDENTIALS_FILE_NAME);
            var credential = await LoadCredentialFromFileAsync(credPath, cancellationToken).ConfigureAwait(false);

            if (credential is not null)
                results.Add(credential);
        }

        return results;
    }

    private static string? ExtractToken(JsonElement root)
    {

        if (root.TryGetProperty(OAUTH_PROPERTY_NAME, out var oauthElement) &&
            oauthElement.ValueKind == JsonValueKind.Object)
        {

            var oauthToken = TryGetTokenString(oauthElement);

            if (!string.IsNullOrWhiteSpace(oauthToken))
                return oauthToken;
        }

        return TryGetTokenString(root);
    }

    private static string? TryGetTokenString(JsonElement element)
    {

        if (element.TryGetProperty(ACCESS_TOKEN_PROPERTY_NAME, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        if (element.TryGetProperty(TOKEN_PROPERTY_NAME, out prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        return null;
    }

    private static DateTimeOffset? ExtractExpiresAt(JsonElement root)
    {

        if (root.TryGetProperty(OAUTH_PROPERTY_NAME, out var oauthElement) &&
            oauthElement.ValueKind == JsonValueKind.Object)
        {

            var expiry = TryGetExpiry(oauthElement);

            if (expiry.HasValue)
                return expiry;
        }

        return TryGetExpiry(root);
    }

    private static DateTimeOffset? TryGetExpiry(JsonElement element)
    {

        if (element.TryGetProperty(EXPIRES_AT_PROPERTY_NAME, out var prop) ||
            element.TryGetProperty(EXPIRES_AT_SNAKE_PROPERTY_NAME, out prop))
            return ParseExpiryElement(prop);

        return null;
    }

    private static DateTimeOffset? ParseExpiryElement(JsonElement element)
    {

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var epochVal))
            return ClaudeEpochTimestampParser.Parse(epochVal);

        if (element.ValueKind == JsonValueKind.String)
        {

            var str = element.GetString();

            if (long.TryParse(str, CultureInfo.InvariantCulture, out var parsedEpoch))
                return ClaudeEpochTimestampParser.Parse(parsedEpoch);

            if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
                return parsedDate;
        }

        return null;
    }

}
