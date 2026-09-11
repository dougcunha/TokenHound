using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Discovers OpenCode API credentials from environment variables or local auth.json storage.
/// </summary>
public sealed class OpenCodeCredentialDiscovery
{
    /// <summary>Non-sensitive source prefix for credentials discovered from environment variables.</summary>
    public const string ENVIRONMENT_SOURCE = "environment";

    /// <summary>Non-sensitive source prefix for credentials discovered from auth.json.</summary>
    public const string AUTH_JSON_SOURCE = "auth.json";

    private const string AUTH_FILE_NAME = "auth.json";
    private const string KEY_PROPERTY_NAME = "key";
    private const string OPENCODE_API_KEY_ENV = "OPENCODE_API_KEY";
    private const string OPENCODE_GO_API_KEY_ENV = "OPENCODE_GO_API_KEY";
    private const string PROVIDER_OPENCODE = "opencode";
    private const string PROVIDER_OPENCODE_GO = "opencode-go";

    private static readonly string[] ENVIRONMENT_VARIABLES =
    [
        OPENCODE_GO_API_KEY_ENV,
        OPENCODE_API_KEY_ENV
    ];

    private static readonly string[] PROVIDER_KEYS =
    [
        PROVIDER_OPENCODE_GO,
        PROVIDER_OPENCODE
    ];

    private readonly string _authFilePath;
    private readonly Func<string, string?> _environmentReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeCredentialDiscovery"/> class.
    /// </summary>
    /// <param name="authFilePath">
    /// The explicit path to the OpenCode <c>auth.json</c> file, or <see langword="null"/> to use the default path.
    /// </param>
    /// <param name="environmentReader">
    /// An optional environment variable reader delegate for testing.
    /// </param>
    public OpenCodeCredentialDiscovery(
        string? authFilePath = null,
        Func<string, string?>? environmentReader = null)
    {

        _authFilePath = string.IsNullOrWhiteSpace(authFilePath)
            ? GetDefaultAuthFilePath()
            : authFilePath;
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    /// <summary>
    /// Gets the resolved path of the <c>auth.json</c> file used for credential discovery.
    /// </summary>
    public string AuthFilePath
        => _authFilePath;

    /// <summary>
    /// Discovers the active OpenCode credential, checking environment variables first
    /// and falling back to the local <c>auth.json</c> file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered <see cref="OpenCodeAuthDto"/> if available; otherwise, <see langword="null"/>.</returns>
    public async Task<OpenCodeAuthDto?> DiscoverAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var credential = DiscoverFromEnvironment()
            ?? await LoadFromFileAsync(_authFilePath, cancellationToken).ConfigureAwait(false);

        if (credential is not null)
            Log.Debug("Discovered OpenCode credential from {Source}", credential.Source);

        return credential;
    }

    /// <summary>
    /// Discovers the active OpenCode credential.
    /// Alias for <see cref="DiscoverAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered <see cref="OpenCodeAuthDto"/> if available; otherwise, <see langword="null"/>.</returns>
    public Task<OpenCodeAuthDto?> DiscoverCredentialAsync(CancellationToken cancellationToken = default)
        => DiscoverAsync(cancellationToken);

    /// <summary>
    /// Reads and parses an OpenCode <c>auth.json</c> file from the specified path.
    /// </summary>
    /// <param name="filePath">The absolute path to the credentials JSON file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The parsed credential if found and valid; otherwise, <see langword="null"/>.</returns>
    public static async Task<OpenCodeAuthDto?> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {

            var jsonContent = await SharedFileReader.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            return ParseAuthJson(jsonContent);
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
    /// Parses OpenCode <c>auth.json</c> content and extracts the first valid key
    /// matching the provider precedence order ("opencode-go", then "opencode").
    /// </summary>
    /// <param name="jsonContent">The raw JSON string to parse.</param>
    /// <returns>The extracted <see cref="OpenCodeAuthDto"/>, or <see langword="null"/> if not found or invalid.</returns>
    public static OpenCodeAuthDto? ParseAuthJson(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            return ExtractFromRoot(root);
        }
        catch (JsonException)
        {

            return null;
        }
    }

    /// <summary>
    /// Gets the default path to the OpenCode credentials file under the current user's profile.
    /// </summary>
    /// <returns>The fully qualified default path to <c>auth.json</c>.</returns>
    public static string GetDefaultAuthFilePath()
    {

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(userProfile))
            userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? ".";

        return Path.Combine(
            userProfile,
            ".local",
            "share",
            "opencode",
            AUTH_FILE_NAME
        );
    }

    private OpenCodeAuthDto? DiscoverFromEnvironment()
    {

        foreach (var variable in ENVIRONMENT_VARIABLES)
        {

            var rawValue = _environmentReader(variable);

            if (!string.IsNullOrWhiteSpace(rawValue))
            {

                return new OpenCodeAuthDto
                {
                    ApiKey = rawValue.Trim(),
                    Source = $"{ENVIRONMENT_SOURCE}:{variable}"
                };
            }
        }

        return null;
    }

    private static OpenCodeAuthDto? ExtractFromRoot(JsonElement root)
    {

        foreach (var providerKey in PROVIDER_KEYS)
        {

            if (!root.TryGetProperty(providerKey, out var providerElement))
                continue;

            var apiKey = ExtractApiKey(providerElement);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {

                return new OpenCodeAuthDto
                {
                    ApiKey = apiKey,
                    Source = $"{AUTH_JSON_SOURCE}:{providerKey}"
                };
            }
        }

        return null;
    }

    private static string? ExtractApiKey(JsonElement element)
    {

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(KEY_PROPERTY_NAME, out var keyElement) &&
            keyElement.ValueKind == JsonValueKind.String)
        {

            var key = keyElement.GetString();

            if (!string.IsNullOrWhiteSpace(key))
                return key.Trim();
        }

        if (element.ValueKind == JsonValueKind.String)
        {

            var rawKey = element.GetString();

            if (!string.IsNullOrWhiteSpace(rawKey))
                return rawKey.Trim();
        }

        return null;
    }
}
