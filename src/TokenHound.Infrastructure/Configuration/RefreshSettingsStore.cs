using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the polling cadence section of the application settings file.
/// </summary>
public sealed class RefreshSettingsStore
{
    private static readonly SectionStore<RefreshSettings> PARSER =
        CreateStore(new UserSettingsFile());

    private readonly SectionStore<RefreshSettings> _store;

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

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public RefreshSettingsStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<RefreshSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Deserializes the polling cadence from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the refresh section.</param>
    /// <returns>The parsed cadence, or an empty instance when the section is absent.</returns>
    public static RefreshSettings FromJson(string json)
        => PARSER.FromJson(json);

    /// <summary>
    /// Loads the configured polling cadence from the settings file.
    /// </summary>
    /// <returns>The stored cadence, or an empty instance when unreadable.</returns>
    public RefreshSettings Load()
        => _store.Load();

    /// <summary>
    /// Asynchronously loads the configured polling cadence from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored cadence, or an empty instance when unreadable.</returns>
    public Task<RefreshSettings> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    /// <summary>
    /// Persists the polling cadence, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The refresh cadence settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(RefreshSettings settings)
        => _store.Save(settings);

    /// <summary>
    /// Asynchronously persists the polling cadence, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The refresh cadence settings to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(RefreshSettings settings, CancellationToken cancellationToken = default)
        => _store.SaveAsync(settings, cancellationToken);

    private static SectionStore<RefreshSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<RefreshSettings>(
            "Refresh",
            settingsFile,
            static settings => settings.Refresh,
            static (settings, value) => settings with { Refresh = value }
        );
}
