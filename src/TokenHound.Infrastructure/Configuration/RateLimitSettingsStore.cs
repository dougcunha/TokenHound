using System;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HTTP 429 rate limit resilience section of the application settings file.
/// </summary>
public sealed class RateLimitSettingsStore
{
    private static readonly SectionStore<RateLimitSettings> PARSER =
        CreateStore(new UserSettingsFile());

    private readonly SectionStore<RateLimitSettings> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettingsStore"/> class using default user settings storage.
    /// </summary>
    public RateLimitSettingsStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettingsStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public RateLimitSettingsStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public RateLimitSettingsStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<RateLimitSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Deserializes the rate limit resilience configuration from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the rate limit section.</param>
    /// <returns>The parsed configuration, or a default instance when the section is absent.</returns>
    public static RateLimitSettings FromJson(string json)
    {
        try
        {

            return Clamp(PARSER.FromJson(json));
        }
        catch (Exception)
        {

            return new RateLimitSettings();
        }
    }

    /// <summary>
    /// Loads the configured rate limit resilience settings from the settings file.
    /// </summary>
    /// <returns>The stored configuration, or a default instance clamped to at least 60 seconds.</returns>
    public RateLimitSettings Load()
        => _store.Load();

    /// <summary>
    /// Asynchronously loads the configured rate limit resilience settings from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored configuration, or a default instance clamped to at least 60 seconds.</returns>
    public Task<RateLimitSettings> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    /// <summary>
    /// Persists the rate limit resilience settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The rate limit settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(RateLimitSettings settings)
        => _store.Save(settings);

    /// <summary>
    /// Asynchronously persists the rate limit resilience settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The rate limit settings to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(RateLimitSettings settings, CancellationToken cancellationToken = default)
        => _store.SaveAsync(settings, cancellationToken);

    private static RateLimitSettings Clamp(RateLimitSettings? settings)
    {

        if (settings is null)
            return new RateLimitSettings();

        if (settings.MinimumRetryFloorSeconds is { } seconds && seconds < RateLimitSettings.MINIMUM_FLOOR_SECONDS)
            return settings with { MinimumRetryFloorSeconds = RateLimitSettings.MINIMUM_FLOOR_SECONDS };

        return settings;
    }

    private static SectionStore<RateLimitSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<RateLimitSettings>(
            "RateLimit",
            settingsFile,
            static settings => Clamp(settings.RateLimit),
            static (settings, value) => settings with { RateLimit = Clamp(value) }
        );
}
