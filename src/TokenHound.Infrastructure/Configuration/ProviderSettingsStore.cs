using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the provider enablement section of the application settings file.
/// </summary>
public sealed class ProviderSettingsStore
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new(SectionStore<ProviderSettings>.SharedOptions)
    {
        Converters = { new UserSettings.ProviderSettingsJsonConverter() }
    };

    private readonly SectionStore<ProviderSettings> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSettingsStore"/> class using default user settings storage.
    /// </summary>
    public ProviderSettingsStore()
        : this(new UserSettingsFile())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSettingsStore"/> class backed by a custom <see cref="UserSettingsFile"/>.
    /// </summary>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    public ProviderSettingsStore(UserSettingsFile settingsFile)
    {

        ArgumentNullException.ThrowIfNull(settingsFile);

        _store = CreateStore(settingsFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSettingsStore"/> class with file path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public ProviderSettingsStore(string? filePath, string? baseDirectory = null)
        : this(SectionStore<ProviderSettings>.ResolveSettingsFile(filePath, baseDirectory))
    {
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _store.FilePath;

    /// <summary>
    /// Deserializes provider enablement from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the providers section.</param>
    /// <returns>The parsed enablement, or an all-enabled instance when the section is absent.</returns>
    public static ProviderSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new ProviderSettings();

        try
        {

            var settings = JsonSerializer.Deserialize<UserSettings>(json, JSON_OPTIONS);

            return settings?.Providers ?? new ProviderSettings();
        }
        catch (Exception)
        {

            return new ProviderSettings();
        }
    }

    /// <summary>
    /// Loads the persisted provider enablement from the settings file.
    /// </summary>
    /// <returns>The stored enablement, or an all-enabled instance when unreadable.</returns>
    public ProviderSettings Load()
        => _store.Load();

    /// <summary>
    /// Asynchronously loads the persisted provider enablement from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The stored enablement, or an all-enabled instance when unreadable.</returns>
    public Task<ProviderSettings> LoadAsync(CancellationToken cancellationToken = default)
        => _store.LoadAsync(cancellationToken);

    /// <summary>
    /// Persists provider enablement, preserving every other settings section and unknown provider keys.
    /// </summary>
    /// <param name="settings">The enablement map to store.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public bool Save(ProviderSettings settings)
        => _store.Save(settings);

    /// <summary>
    /// Asynchronously persists provider enablement, preserving every other settings section and unknown provider keys.
    /// </summary>
    /// <param name="settings">The enablement map to store.</param>
    /// <param name="cancellationToken">Token cancelling the write operation.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public Task<bool> SaveAsync(ProviderSettings settings, CancellationToken cancellationToken = default)
        => _store.SaveAsync(settings, cancellationToken);

    private static UserSettings MergeProviders(UserSettings current, ProviderSettings settings)
    {

        var merged = MergeStates(current.Providers, settings);

        return current with { Providers = new ProviderSettings { EnabledStates = merged } };
    }

    private static Dictionary<string, bool> MergeStates(
        ProviderSettings? currentProviders,
        ProviderSettings newSettings)
    {

        var merged = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (currentProviders?.EnabledStates is { } existing)
        {

            foreach (var (key, isEnabled) in existing)
                merged[key] = isEnabled;
        }

        foreach (var (key, isEnabled) in newSettings.EnabledStates)
            merged[key] = isEnabled;

        return merged;
    }

    private static SectionStore<ProviderSettings> CreateStore(UserSettingsFile settingsFile)
        => new SectionStore<ProviderSettings>(
            "Providers",
            settingsFile,
            static settings => settings.Providers,
            static (settings, value) => MergeProviders(settings, value)
        );
}
