using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HUD placement section of the application settings file.
/// </summary>
public sealed class HudPositionStore
{
    private static readonly SectionStore<HudPositionSettings> PARSER =
        CreateStore(new UserSettingsFile());

    private readonly SectionStore<HudPositionSettings> _store;

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

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPositionStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public HudPositionStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<HudPositionSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Deserializes the HUD placement from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the HUD section.</param>
    /// <returns>The parsed placement, or an empty instance when the section is absent.</returns>
    public static HudPositionSettings FromJson(string json)
        => PARSER.FromJson(json);

    /// <summary>
    /// Loads the persisted HUD placement from the settings file.
    /// </summary>
    /// <returns>The stored placement, or an empty instance when unreadable.</returns>
    public HudPositionSettings Load()
        => _store.Load();

    /// <summary>
    /// Asynchronously loads the persisted HUD placement from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the load operation.</param>
    /// <returns>The stored placement, or an empty instance when unreadable.</returns>
    public Task<HudPositionSettings> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    /// <summary>
    /// Persists the HUD placement, preserving every other settings section.
    /// </summary>
    /// <param name="position">The placement to store.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public bool Save(HudPositionSettings position)
        => _store.Save(position);

    /// <summary>
    /// Asynchronously persists the HUD placement, preserving every other settings section.
    /// </summary>
    /// <param name="position">The placement to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(HudPositionSettings position, CancellationToken cancellationToken = default)
        => _store.SaveAsync(position, cancellationToken);

    private static SectionStore<HudPositionSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<HudPositionSettings>(
            "Hud",
            settingsFile,
            static settings => settings.Hud,
            static (settings, value) => settings with { Hud = value }
        );
}
