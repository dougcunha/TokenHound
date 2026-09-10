using System;
using System.IO;
using System.Text.Json;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Loads and deserializes logging configuration from JSON files or strings.
/// </summary>
public static class LogConfigurationLoader
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";
    private const string LOG_SECTION_NAME = "Log";

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads logging settings from the specified JSON file or defaults to appsettings.json.
    /// </summary>
    /// <param name="filePath">Optional file path to inspect.</param>
    /// <param name="baseDirectory">Optional base directory for relative path resolution.</param>
    /// <returns>A populated or default <see cref="LogSettings"/> instance.</returns>
    public static LogSettings Load(string? filePath = null, string? baseDirectory = null)
    {

        var targetPath = SettingsPathResolver.Resolve(
            filePath,
            baseDirectory,
            DEFAULT_CONFIG_FILE
        );

        if (!File.Exists(targetPath))
            return new LogSettings();

        try
        {

            var json = File.ReadAllText(targetPath);

            return FromJson(json);
        }
        catch (Exception)
        {

            return new LogSettings();
        }
    }

    /// <summary>
    /// Deserializes logging settings from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the logging section or options.</param>
    /// <returns>A populated <see cref="LogSettings"/> instance.</returns>
    public static LogSettings FromJson(string json)
    {

        if (string.IsNullOrWhiteSpace(json))
            return new LogSettings();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty(LOG_SECTION_NAME, out var logSection))
            return JsonSerializer.Deserialize<LogSettings>(logSection.GetRawText(), JSON_OPTIONS) ?? new LogSettings();

        return JsonSerializer.Deserialize<LogSettings>(json, JSON_OPTIONS) ?? new LogSettings();
    }

}
