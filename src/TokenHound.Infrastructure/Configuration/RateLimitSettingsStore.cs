using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HTTP 429 rate limit resilience section of the application settings file.
/// </summary>
public sealed class RateLimitSettingsStore
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string RATE_LIMIT_SECTION_NAME = "RateLimit";

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

        _settingsFile = settingsFile;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public RateLimitSettingsStore(string? filePath, string? baseDirectory = null)
        : this(CreateSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _settingsFile.UserSettingsPath;

    /// <summary>
    /// Deserializes the rate limit resilience configuration from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the rate limit section.</param>
    /// <returns>The parsed configuration, or a default instance when the section is absent.</returns>
    public static RateLimitSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new RateLimitSettings();

        try
        {

            using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

            if (!document.RootElement.TryGetProperty(RATE_LIMIT_SECTION_NAME, out var rateLimitSection))
                return new RateLimitSettings();

            var settings = JsonSerializer.Deserialize<RateLimitSettings>(rateLimitSection.GetRawText(), JSON_OPTIONS)
                   ?? new RateLimitSettings();

            return Clamp(settings);
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
        => Clamp(_settingsFile.Load().RateLimit);

    /// <summary>
    /// Asynchronously loads the configured rate limit resilience settings from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored configuration, or a default instance clamped to at least 60 seconds.</returns>
    public async Task<RateLimitSettings> LoadAsync(CancellationToken cancellationToken = default)
    {

        var settings = await _settingsFile.LoadAsync(cancellationToken).ConfigureAwait(false);

        return Clamp(settings.RateLimit);
    }

    /// <summary>
    /// Persists the rate limit resilience settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The rate limit settings to store.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public bool Save(RateLimitSettings settings)
    {

        ArgumentNullException.ThrowIfNull(settings);

        var clamped = Clamp(settings);

        return _settingsFile.Update(current => current with { RateLimit = clamped });
    }

    /// <summary>
    /// Asynchronously persists the rate limit resilience settings, preserving every other settings section.
    /// </summary>
    /// <param name="settings">The rate limit settings to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the settings were saved; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(RateLimitSettings settings, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(settings);

        var clamped = Clamp(settings);

        return _settingsFile.UpdateAsync(
            current => current with { RateLimit = clamped },
            cancellationToken);
    }

    private static RateLimitSettings Clamp(RateLimitSettings? settings)
    {

        if (settings is null)
            return new RateLimitSettings();

        if (settings.MinimumRetryFloorSeconds is { } seconds && seconds < RateLimitSettings.MINIMUM_FLOOR_SECONDS)
            return settings with { MinimumRetryFloorSeconds = RateLimitSettings.MINIMUM_FLOOR_SECONDS };

        return settings;
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
