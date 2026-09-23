using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Discovers and parses Claude Code credentials from default and multi-profile directories.
/// </summary>
public sealed partial class ClaudeProfileDiscovery
{
    private const string CREDENTIALS_FILE_NAME = ".credentials.json";
    private const string DEFAULT_PROFILE_DIR = ".claude";
    private const string PROFILE_PATTERN = ".claude-*";
    private const string PROFILE_PREFIX = ".claude-";

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
    /// Discovers all Claude Code profiles present under the base directory.
    /// </summary>
    /// <param name="onlyActive">
    /// <see langword="true"/> to return only profiles with valid credentials; <see langword="false"/> to return all found folders.
    /// </param>
    /// <returns>A read-only list of discovered <see cref="ClaudeProfile"/> instances.</returns>
    public IReadOnlyList<ClaudeProfile> DiscoverProfiles(bool onlyActive = true)
    {

        var profiles = new List<ClaudeProfile>();
        var defaultDir = Path.Combine(_baseDirectory, DEFAULT_PROFILE_DIR);

        if (!onlyActive || HasCredentials(defaultDir))
            profiles.Add(new ClaudeProfile("claude", "Claude Code", defaultDir, null));

        if (!Directory.Exists(_baseDirectory))
            return profiles;

        var matchingDirs = Directory.GetDirectories(_baseDirectory, PROFILE_PATTERN)
            .OrderBy(static dir => dir, StringComparer.OrdinalIgnoreCase);

        foreach (var dir in matchingDirs)
        {

            var dirName = Path.GetFileName(dir);

            if (string.IsNullOrWhiteSpace(dirName) || dirName.Length <= PROFILE_PREFIX.Length)
                continue;

            var slug = dirName[PROFILE_PREFIX.Length..];

            if (string.IsNullOrWhiteSpace(slug))
                continue;

            if (!onlyActive || HasCredentials(dir))
                profiles.Add(new ClaudeProfile($"claude-{slug.ToLowerInvariant()}", $"Claude Code ({slug})", dir, slug));
        }

        return profiles;
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

    private static bool HasCredentials(string directoryPath)
    {

        var credPath = Path.Combine(directoryPath, CREDENTIALS_FILE_NAME);

        if (!File.Exists(credPath))
            return false;

        try
        {

            using var stream = SharedFileReader.OpenRead(credPath);
            using var reader = new StreamReader(stream);

            return ParseCredentialJson(reader.ReadToEnd()) is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return false;
        }
    }

    private async Task<IReadOnlyList<ClaudeCredentialDto>> DiscoverMultiProfileCredentialsAsync(CancellationToken cancellationToken)
    {

        var profiles = DiscoverProfiles(onlyActive: true);
        var results = new List<ClaudeCredentialDto>();

        foreach (var profile in profiles)
        {

            if (profile.Slug is null)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            var credPath = Path.Combine(profile.DirectoryPath, CREDENTIALS_FILE_NAME);
            var credential = await LoadCredentialFromFileAsync(credPath, cancellationToken).ConfigureAwait(false);

            if (credential is not null)
                results.Add(credential);
        }

        return results;
    }
}
