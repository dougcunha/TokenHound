using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the automatic update section of the application settings file.
/// </summary>
public sealed class UpdateSettingsStore
{
    private const string SECTION_NAME = "Update";

    private static readonly SectionStore<UpdateSettings> PARSER =
        CreateStore(new UserSettingsFile());

    private readonly SectionStore<UpdateSettings> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsStore"/> class using default user settings storage.
    /// </summary>
    public UpdateSettingsStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public UpdateSettingsStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public UpdateSettingsStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<UpdateSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Deserializes the update settings from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the update section.</param>
    /// <returns>The parsed settings, or a default instance when the section is absent or invalid.</returns>
    public static UpdateSettings FromJson(string json)
    {
        try
        {

            return Clamp(PARSER.FromJson(json));
        }
        catch (Exception)
        {

            return new UpdateSettings();
        }
    }

    /// <summary>
    /// Loads the update settings from the settings file.
    /// </summary>
    /// <returns>The stored settings with the interval clamped, or a default instance.</returns>
    public UpdateSettings Load()
        => _store.Load();

    /// <summary>
    /// Asynchronously loads the update settings from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored settings with the interval clamped, or a default instance.</returns>
    public Task<UpdateSettings> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    /// <summary>
    /// Persists the update settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The update settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(UpdateSettings settings)
        => _store.Save(settings);

    /// <summary>
    /// Asynchronously persists the update settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The update settings to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(UpdateSettings settings, CancellationToken cancellationToken = default)
        => _store.SaveAsync(settings, cancellationToken);

    private static UpdateSettings Clamp(UpdateSettings? settings)
    {

        if (settings is null)
            return new UpdateSettings();

        if (settings.CheckIntervalHours is { } hours && hours != settings.IntervalHours)
            return settings with { CheckIntervalHours = settings.IntervalHours };

        return settings;
    }

    private static SectionStore<UpdateSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<UpdateSettings>(
            SECTION_NAME,
            settingsFile,
            static settings => Clamp(settings.Update),
            static (settings, value) => settings with { Update = Clamp(value) }
        );
}
