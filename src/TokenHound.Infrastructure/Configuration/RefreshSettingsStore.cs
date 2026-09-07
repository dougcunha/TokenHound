using System;
using System.IO;
using System.Text.Json;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Reads the polling cadence section of the application settings file.
/// </summary>
public sealed class RefreshSettingsStore
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string REFRESH_SECTION_NAME = "Refresh";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonDocumentOptions DOCUMENT_OPTIONS = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSettingsStore"/> class.
    /// </summary>
    /// <param name="filePath">Optional settings file path; defaults to appsettings.json.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    public RefreshSettingsStore(string? filePath = null, string? baseDirectory = null)
    {

        _filePath = ResolveFilePath(filePath, baseDirectory);
    }

    /// <summary>
    /// Gets the resolved settings file path backing this store.
    /// </summary>
    public string FilePath
        => _filePath;

    /// <summary>
    /// Deserializes the polling cadence from a settings JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the refresh section.</param>
    /// <returns>The parsed cadence, or an empty instance when the section is absent.</returns>
    public static RefreshSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new RefreshSettings();

        using var document = JsonDocument.Parse(json, DOCUMENT_OPTIONS);

        if (!document.RootElement.TryGetProperty(REFRESH_SECTION_NAME, out var refreshSection))
            return new RefreshSettings();

        return JsonSerializer.Deserialize<RefreshSettings>(refreshSection.GetRawText(), JSON_OPTIONS)
               ?? new RefreshSettings();
    }

    /// <summary>
    /// Loads the configured polling cadence from the settings file.
    /// </summary>
    /// <returns>The stored cadence, or an empty instance when unreadable.</returns>
    public RefreshSettings Load()
    {

        if (!File.Exists(_filePath))
            return new RefreshSettings();

        try
        {

            return FromJson(File.ReadAllText(_filePath));
        }
        catch (Exception)
        {

            return new RefreshSettings();
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
}
