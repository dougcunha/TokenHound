using System;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HUD size section of the application settings file.
/// </summary>
public sealed class HudSizeStore
{
    private readonly SectionStore<HudSizeSettings> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudSizeStore"/> class using default user settings storage.
    /// </summary>
    public HudSizeStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudSizeStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public HudSizeStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudSizeStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public HudSizeStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<HudSizeSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Loads the configured HUD size from the settings file.
    /// </summary>
    /// <returns>The stored size, or an empty instance when unreadable.</returns>
    public HudSizeSettings Load()
        => _store.Load();

    /// <summary>
    /// Persists the HUD size, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The HUD size settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(HudSizeSettings settings)
        => _store.Save(settings);

    private static SectionStore<HudSizeSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<HudSizeSettings>(
            "HudSize",
            settingsFile,
            static settings => settings.HudSize,
            static (settings, value) => settings with { HudSize = value }
        );
}
