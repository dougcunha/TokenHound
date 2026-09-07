using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the HUD placement section of the application settings file.
/// </summary>
public sealed class HudPositionStore
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string HUD_SECTION_NAME = "Hud";
    private const string LEFT_PROPERTY_NAME = "Left";
    private const string TOP_PROPERTY_NAME = "Top";

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

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudPositionStore"/> class.
    /// </summary>
    /// <param name="filePath">Optional settings file path; defaults to appsettings.json.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public HudPositionStore(string? filePath = null, string? baseDirectory = null)
    {

        _filePath = ResolveFilePath(filePath, baseDirectory);
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _filePath;

    /// <summary>
    /// Deserializes the HUD placement from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the HUD section.</param>
    /// <returns>The parsed placement, or an empty instance when the section is absent.</returns>
    public static HudPositionSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new HudPositionSettings();

        using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

        if (!document.RootElement.TryGetProperty(HUD_SECTION_NAME, out var hudSection))
            return new HudPositionSettings();

        return JsonSerializer.Deserialize<HudPositionSettings>(hudSection.GetRawText(), JSON_OPTIONS)
               ?? new HudPositionSettings();
    }

    /// <summary>
    /// Loads the persisted HUD placement from the settings file.
    /// </summary>
    /// <returns>The stored placement, or an empty instance when unreadable.</returns>
    public HudPositionSettings Load()
    {

        if (!File.Exists(_filePath))
            return new HudPositionSettings();

        try
        {

            return FromJson(File.ReadAllText(_filePath));
        }
        catch (Exception)
        {

            return new HudPositionSettings();
        }
    }

    /// <summary>
    /// Persists the HUD placement, preserving every other settings section.
    /// </summary>
    /// <param name="position">The placement to store.</param>
    /// <returns><see langword="true"/> when the file was written; otherwise <see langword="false"/>.</returns>
    public bool Save(HudPositionSettings position)
    {

        ArgumentNullException.ThrowIfNull(position);

        try
        {

            var root = ReadRoot();

            root[HUD_SECTION_NAME] = new JsonObject
            {
                [LEFT_PROPERTY_NAME] = position.Left,
                [TOP_PROPERTY_NAME] = position.Top
            };

            File.WriteAllText(_filePath, root.ToJsonString(JSON_OPTIONS));

            return true;
        }
        catch (Exception)
        {

            return false;
        }
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

    private JsonObject ReadRoot()
    {

        if (!File.Exists(_filePath))
            return new JsonObject();

        var json = File.ReadAllText(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new JsonObject();

        return JsonNode.Parse(json, documentOptions: DOCUMENT_OPTIONS) as JsonObject
               ?? new JsonObject();
    }
}
