using System;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HUD backdrop section of the application settings file.
/// </summary>
public sealed class HudBackdropStore
{
    private readonly SectionStore<HudBackdropSettings> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudBackdropStore"/> class using default user settings storage.
    /// </summary>
    public HudBackdropStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudBackdropStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public HudBackdropStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudBackdropStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public HudBackdropStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<HudBackdropSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Loads the HUD backdrop preference from the settings file.
    /// </summary>
    /// <returns>The stored preference, or an empty instance (enabled) when absent or unreadable.</returns>
    public HudBackdropSettings Load()
        => _store.Load();

    /// <summary>
    /// Persists the HUD backdrop preference, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The HUD backdrop settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(HudBackdropSettings settings)
        => _store.Save(settings);

    private static SectionStore<HudBackdropSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<HudBackdropSettings>(
            "HudBackdrop",
            settingsFile,
            static settings => settings.HudBackdrop,
            static (settings, value) => settings with { HudBackdrop = value }
        );
}
