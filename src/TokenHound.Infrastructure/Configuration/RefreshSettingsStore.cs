using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the polling cadence section of the application settings file.
/// </summary>
public sealed class RefreshSettingsStore
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string REFRESH_SECTION_NAME = "Refresh";

    private static readonly JsonDocumentOptions DOCUMENT_OPTIONS = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    private readonly UserSettingsFile _settingsFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStore"/> class using default user settings storage.
    /// </summary>
    public RefreshSettingsStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public RefreshSettingsStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _settingsFile = settingsFile;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public RefreshSettingsStore(string? filePath, string? baseDirectory = null)
        : this(CreateSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _settingsFile.UserSettingsPath;

    /// <summary>
    /// Deserializes the polling cadence from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the refresh section.</param>
    /// <returns>The parsed cadence, or an empty instance when the section is absent.</returns>
    public static RefreshSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new RefreshSettings();

        using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

        if (!document.RootElement.TryGetProperty(REFRESH_SECTION_NAME, out var refreshSection))
            return new RefreshSettings();

        return JsonSerializer.Deserialize<RefreshSettings>(refreshSection.GetRawText(), JSON_OPTIONS)
               ?? new RefreshSettings();
    }

    /// <summary>
    /// Loads the configured polling cadence from the settings file.
    /// </summary>
    /// <returns>The stored cadence, or an empty instance when unreadable.</returns>
    public RefreshSettings Load()
        => _settingsFile.Load().Refresh ?? new RefreshSettings();

    /// <summary>
    /// Asynchronously loads the configured polling cadence from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored cadence, or an empty instance when unreadable.</returns>
    public async Task<RefreshSettings> LoadAsync(CancellationToken cancellationToken = default)
    {

        var settings = await _settingsFile.LoadAsync(cancellationToken).ConfigureAwait(false);

        return settings.Refresh ?? new RefreshSettings();
    }

    /// <summary>
    /// Persists the polling cadence, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The refresh cadence settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(RefreshSettings settings)
    {

        ArgumentNullException.ThrowIfNull(settings);

        return _settingsFile.Update(current => current with { Refresh = settings });
    }

    /// <summary>
    /// Asynchronously persists the polling cadence, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The refresh cadence settings to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(RefreshSettings settings, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(settings);

        return _settingsFile.UpdateAsync(
            current => current with { Refresh = settings },
            cancellationToken);
    }

    private static UserSettingsFile CreateSettingsFile(string? filePath, string? baseDirectory)
    {

        var resolvedPath = SettingsPathResolver.ResolveOverride(
            filePath,
            baseDirectory,
            DEFAULT_CONFIG_FILE
        );

        return new UserSettingsFile(userSettingsPath: resolvedPath);
    }
}
