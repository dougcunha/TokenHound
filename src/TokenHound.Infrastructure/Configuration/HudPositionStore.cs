using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HUD placement section of the application settings file.
/// </summary>
public sealed class HudPositionStore
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string HUD_SECTION_NAME = "Hud";

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
    /// Initializes a new instance of the <see cref="HudPositionStore"/> class using default user settings storage.
    /// </summary>
    public HudPositionStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPositionStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public HudPositionStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _settingsFile = settingsFile;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPositionStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public HudPositionStore(string? filePath, string? baseDirectory = null)
        : this(CreateSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _settingsFile.UserSettingsPath;

    /// <summary>
    /// Deserializes the HUD placement from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the HUD section.</param>
    /// <returns>The parsed placement, or an empty instance when the section is absent.</returns>
    public static HudPositionSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new HudPositionSettings();

        using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

        if (!document.RootElement.TryGetProperty(HUD_SECTION_NAME, out var hudSection))
            return new HudPositionSettings();

        return JsonSerializer.Deserialize<HudPositionSettings>(hudSection.GetRawText(), JSON_OPTIONS)
               ?? new HudPositionSettings();
    }

    /// <summary>
    /// Loads the persisted HUD placement from the settings file.
    /// </summary>
    /// <returns>The stored placement, or an empty instance when unreadable.</returns>
    public HudPositionSettings Load()
        => _settingsFile.Load().Hud ?? new HudPositionSettings();

    /// <summary>
    /// Asynchronously loads the persisted HUD placement from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the load operation.</param>
    /// <returns>The stored placement, or an empty instance when unreadable.</returns>
    public async Task<HudPositionSettings> LoadAsync(CancellationToken cancellationToken = default)
    {

        var settings = await _settingsFile.LoadAsync(cancellationToken).ConfigureAwait(false);

        return settings.Hud ?? new HudPositionSettings();
    }

    /// <summary>
    /// Persists the HUD placement, preserving every other settings section.
    /// </summary>
    /// <param name="position">The placement to store.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public bool Save(HudPositionSettings position)
    {

        ArgumentNullException.ThrowIfNull(position);

        return _settingsFile.Update(current => current with { Hud = position });
    }

    /// <summary>
    /// Asynchronously persists the HUD placement, preserving every other settings section.
    /// </summary>
    /// <param name="position">The placement to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(HudPositionSettings position, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(position);

        return _settingsFile.UpdateAsync(
            current => current with { Hud = position },
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
