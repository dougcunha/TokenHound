using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the provider enablement section of the application settings file.
/// </summary>
public sealed class ProviderSettingsStore
{
    private const string ALIAS_PROVIDER_KEY = "antigravity";
    private const string CANONICAL_PROVIDER_KEY = "gemini";
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string ENABLED_PROPERTY_NAME = "Enabled";
    private const string PROVIDERS_SECTION_NAME = "Providers";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions DOCUMENT_OPTIONS = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonNodeOptions NODE_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSettingsStore"/> class.
    /// </summary>
    /// <param name="filePath">Optional settings file path; defaults to appsettings.json.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public ProviderSettingsStore(string? filePath = null, string? baseDirectory = null)
    {

        _filePath = ResolveFilePath(filePath, baseDirectory);
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _filePath;

    /// <summary>
    /// Deserializes provider enablement from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the providers section.</param>
    /// <returns>The parsed enablement, or an all-enabled instance when the section is absent.</returns>
    public static ProviderSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new ProviderSettings();

        if (JsonNode.Parse(json, NODE_OPTIONS, DOCUMENT_OPTIONS) is not JsonObject root)
            return new ProviderSettings();

        if (root[PROVIDERS_SECTION_NAME] is not JsonObject section)
            return new ProviderSettings();

        return new ProviderSettings { EnabledStates = ReadStates(section) };
    }

    /// <summary>
    /// Loads the persisted provider enablement from the settings file.
    /// </summary>
    /// <returns>The stored enablement, or an all-enabled instance when unreadable.</returns>
    public ProviderSettings Load()
    {

        if (!File.Exists(_filePath))
            return new ProviderSettings();

        try
        {

            return FromJson(File.ReadAllText(_filePath));
        }
        catch (Exception)
        {

            return new ProviderSettings();
        }
    }

    /// <summary>
    /// Persists provider enablement, preserving every other settings section and unknown provider keys.
    /// </summary>
    /// <param name="settings">The enablement map to store.</param>
    /// <param name="cancellationToken">Token cancelling the write.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public async Task<bool> SaveAsync(ProviderSettings settings, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(settings);

        try
        {

            using var gate = await SettingsFileGate.AcquireAsync(_filePath, cancellationToken).ConfigureAwait(false);

            var root = await ReadRootAsync(cancellationToken).ConfigureAwait(false);

            ApplyStates(root, settings);

            await File
                .WriteAllTextAsync(_filePath, root.ToJsonString(JSON_OPTIONS), cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {

            Log.Warning(ex, "Failed to persist provider enablement to {SettingsFilePath}", _filePath);

            return false;
        }
    }

    private static void ApplyStates(JsonObject root, ProviderSettings settings)
    {

        if (root[PROVIDERS_SECTION_NAME] is not JsonObject section)
        {

            section = new JsonObject(NODE_OPTIONS);
            root[PROVIDERS_SECTION_NAME] = section;
        }

        foreach (var state in settings.EnabledStates)
        {

            var providerId = NormalizeKey(state.Key);

            if (providerId.Length == 0)
                continue;

            WriteEnabled(section, providerId, state.Value);
        }
    }

    private static bool IsAlias(string key)
        => string.Equals(key, ALIAS_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKey(string? key)
    {

        var trimmed = key?.Trim() ?? string.Empty;

        return IsAlias(trimmed) ? CANONICAL_PROVIDER_KEY : trimmed;
    }

    private static bool ReadEnabled(JsonNode? entry)
    {

        if (entry is not JsonObject provider)
            return true;

        if (provider[ENABLED_PROPERTY_NAME] is not JsonValue value)
            return true;

        return !value.TryGetValue<bool>(out var isEnabled) || isEnabled;
    }

    private static Dictionary<string, bool> ReadStates(JsonObject section)
    {

        var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in section)
        {

            var providerId = NormalizeKey(entry.Key);

            if (providerId.Length == 0)
                continue;

            if (IsAlias(entry.Key.Trim()) && states.ContainsKey(CANONICAL_PROVIDER_KEY))
                continue;

            states[providerId] = ReadEnabled(entry.Value);
        }

        return states;
    }

    private static string ResolveFilePath(string? filePath, string? baseDirectory)
    {

        if (!string.IsNullOrWhiteSpace(filePath) && Path.IsPathRooted(filePath))
            return filePath;

        var baseDir = !string.IsNullOrWhiteSpace(baseDirectory)
            ? baseDirectory
            : AppContext.BaseDirectory;

        return Path.Combine(baseDir, filePath ?? DEFAULT_CONFIG_FILE);
    }

    private static void WriteEnabled(JsonObject section, string providerId, bool isEnabled)
    {

        if (string.Equals(providerId, CANONICAL_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase))
            section.Remove(ALIAS_PROVIDER_KEY);

        if (section[providerId] is JsonObject provider)
        {

            provider[ENABLED_PROPERTY_NAME] = JsonValue.Create(isEnabled);

            return;
        }

        section[providerId] = new JsonObject(NODE_OPTIONS)
        {
            [ENABLED_PROPERTY_NAME] = JsonValue.Create(isEnabled)
        };
    }

    private async Task<JsonObject> ReadRootAsync(CancellationToken cancellationToken)
    {

        if (!File.Exists(_filePath))
            return new JsonObject(NODE_OPTIONS);

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
            return new JsonObject(NODE_OPTIONS);

        return JsonNode.Parse(json, NODE_OPTIONS, DOCUMENT_OPTIONS) as JsonObject
               ?? new JsonObject(NODE_OPTIONS);
    }
}
