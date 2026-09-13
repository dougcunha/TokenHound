using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Owns the shared JSON options, path resolution, section name, and persistence plumbing for one settings section.
/// </summary>
/// <typeparam name="T">The settings section payload type.</typeparam>
internal sealed class SectionStore<T>
    where T : class, new()
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    /// <summary>
    /// Gets the shared serializer options, exposed so stores that need an extra converter can clone them.
    /// </summary>
    internal static JsonSerializerOptions SharedOptions
        => JSON_OPTIONS;

    private readonly Func<UserSettings, T?> _readSection;
    private readonly string _sectionName;
    private readonly UserSettingsFile _settingsFile;
    private readonly Func<UserSettings, T, UserSettings> _writeSection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectionStore{T}"/> class.
    /// </summary>
    /// <param name="sectionName">The root JSON property name owned by this store.</param>
    /// <param name="settingsFile">The underlying settings persistence manager.</param>
    /// <param name="readSection">Reads the typed section from the root settings.</param>
    /// <param name="writeSection">Replaces the typed section on the root settings.</param>
    internal SectionStore(
        string sectionName,
        UserSettingsFile settingsFile,
        Func<UserSettings, T?> readSection,
        Func<UserSettings, T, UserSettings> writeSection)
    {

        _sectionName = sectionName;
        _settingsFile = settingsFile;
        _readSection = readSection;
        _writeSection = writeSection;
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    internal string FilePath
        => _settingsFile.UserSettingsPath;

    /// <summary>
    /// Resolves a settings file from optional path overrides.
    /// </summary>
    /// <param name="filePath">Optional settings file path override.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    /// <returns>The resolved settings persistence manager.</returns>
    internal static UserSettingsFile ResolveSettingsFile(string? filePath, string? baseDirectory)
        => CreateSettingsFile(filePath, baseDirectory);

    /// <summary>
    /// Deserializes this store's section from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the section.</param>
    /// <returns>The parsed section, or an empty instance when the section is absent.</returns>
    internal T FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new T();

        var root = JsonSerializer.Deserialize<JsonElement>(json, JSON_OPTIONS);

        if (!root.TryGetProperty(_sectionName, out var section))
            return new T();

        return JsonSerializer.Deserialize<T>(section, JSON_OPTIONS) ?? new T();
    }

    /// <summary>
    /// Loads the persisted section from the settings file.
    /// </summary>
    /// <returns>The stored section, or an empty instance when unreadable.</returns>
    internal T Load()
        => _readSection(_settingsFile.Load()) ?? new T();

    /// <summary>
    /// Asynchronously loads the persisted section from the settings file.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the load operation.</param>
    /// <returns>The stored section, or an empty instance when unreadable.</returns>
    internal async Task<T> LoadAsync(CancellationToken cancellationToken = default)
    {

        var settings = await _settingsFile.LoadAsync(cancellationToken).ConfigureAwait(false);

        return _readSection(settings) ?? new T();
    }

    /// <summary>
    /// Persists the section, preserving every other settings section.
    /// </summary>
    /// <param name="value">The section value to store.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    internal bool Save(T value)
    {

        ArgumentNullException.ThrowIfNull(value);

        return _settingsFile.Update(current => _writeSection(current, value));
    }

    /// <summary>
    /// Asynchronously persists the section, preserving every other settings section.
    /// </summary>
    /// <param name="value">The section value to store.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    internal Task<bool> SaveAsync(T value, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(value);

        return _settingsFile.UpdateAsync(
            current => _writeSection(current, value),
            cancellationToken
        );
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
