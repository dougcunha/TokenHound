using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Discovers borrowed Cline credentials from the environment or from the CLI/desktop
/// <c>providers.json</c> store, without ever writing to it.
/// </summary>
/// <remarks>
/// The Cline CLI and desktop app share <c>%USERPROFILE%\.cline\data\settings\providers.json</c>. The
/// stored access token lives for one hour and is refreshed and rewritten by Cline itself; TokenHound
/// only reads the current value and never refreshes or persists it.
/// </remarks>
public sealed class ClineCredentialDiscovery
{
    /// <summary>Non-sensitive source prefix for credentials discovered from environment variables.</summary>
    public const string ENVIRONMENT_SOURCE = "environment";

    /// <summary>The environment variable that overrides the stored Cline credential.</summary>
    public const string CLINE_API_KEY_ENV = "CLINE_API_KEY";

    private const string CLINE_CONFIG_DIRECTORY = ".cline";
    private const string DATA_DIRECTORY = "data";
    private const string PROVIDERS_FILE_NAME = "providers.json";
    private const string SETTINGS_DIRECTORY = "settings";
    private const string WORKOS_TOKEN_PREFIX = "workos:";

    private readonly string _providersFilePath;
    private readonly Func<string, string?> _environmentReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineCredentialDiscovery"/> class.
    /// </summary>
    /// <param name="providersFilePath">
    /// The explicit path to the Cline <c>providers.json</c> file, or <see langword="null"/> to use the default path.
    /// </param>
    /// <param name="environmentReader">An optional environment variable reader delegate for testing.</param>
    public ClineCredentialDiscovery(
        string? providersFilePath = null,
        Func<string, string?>? environmentReader = null)
    {

        _providersFilePath = string.IsNullOrWhiteSpace(providersFilePath)
            ? GetDefaultProvidersFilePath()
            : providersFilePath;
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
    }

    /// <summary>
    /// Gets the resolved path of the <c>providers.json</c> file used for credential discovery.
    /// </summary>
    public string ProvidersFilePath
        => _providersFilePath;

    /// <summary>
    /// Discovers the active Cline credential, checking the environment first and falling back to the
    /// local <c>providers.json</c> store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered <see cref="ClineAuthDto"/> if available; otherwise, <see langword="null"/>.</returns>
    public async Task<ClineAuthDto?> DiscoverAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var credential = DiscoverFromEnvironment()
            ?? await LoadFromFileAsync(_providersFilePath, cancellationToken).ConfigureAwait(false);

        if (credential is not null)
            Log.Debug("Discovered Cline credential from {Source}", credential.Source);

        return credential;
    }

    /// <summary>
    /// Discovers the active Cline credential.
    /// Alias for <see cref="DiscoverAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered <see cref="ClineAuthDto"/> if available; otherwise, <see langword="null"/>.</returns>
    public Task<ClineAuthDto?> DiscoverCredentialAsync(CancellationToken cancellationToken = default)
        => DiscoverAsync(cancellationToken);

    /// <summary>
    /// Reads and parses a Cline <c>providers.json</c> file from the specified path.
    /// </summary>
    /// <param name="filePath">The absolute path to the Cline provider store.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The parsed credential if found and valid; otherwise, <see langword="null"/>.</returns>
    public static async Task<ClineAuthDto?> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {

            var jsonContent = await SharedFileReader.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            return ClineProvidersJson.Parse(jsonContent);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            // Cline rewrites this file when it rotates the token; a partial read is retried next poll.
            return null;
        }
    }

    /// <summary>
    /// Gets the default path of the Cline provider store under the current user's profile.
    /// </summary>
    /// <returns>The fully qualified default path to <c>providers.json</c>.</returns>
    public static string GetDefaultProvidersFilePath()
    {

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(userProfile))
            userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? ".";

        return Path.Combine(
            userProfile,
            CLINE_CONFIG_DIRECTORY,
            DATA_DIRECTORY,
            SETTINGS_DIRECTORY,
            PROVIDERS_FILE_NAME
        );
    }

    private ClineAuthDto? DiscoverFromEnvironment()
    {

        var rawValue = _environmentReader(CLINE_API_KEY_ENV);

        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var trimmed = rawValue.Trim();

        return new ClineAuthDto
        {
            AccessToken = trimmed.StartsWith(WORKOS_TOKEN_PREFIX, StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"{WORKOS_TOKEN_PREFIX}{trimmed}",
            Source = $"{ENVIRONMENT_SOURCE}:{CLINE_API_KEY_ENV}"
        };
    }
}
