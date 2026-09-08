using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Manages atomic persistence and fallback resolution for user settings in LocalAppData.
/// </summary>
public sealed class UserSettingsFile
{
    private const string DEFAULT_CONFIG_FILE = "appsettings.json";

    private static readonly HashSet<string> DEFAULT_PROVIDERS = new(
        ["antigravity", "claude", "codex", "copilot", "cursor", "gemini"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        AllowTrailingCommas = true,
        Converters = { new UserSettings.ProviderSettingsJsonConverter() },
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    /// <summary>Gets the default per-user settings file path under LocalAppData.</summary>
    public static string DefaultUserSettingsPath { get; }
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TokenHound", "settings.json");

    /// <summary>Gets the resolved user settings file path.</summary>
    public string UserSettingsPath { get; }

    /// <summary>Gets the resolved defaults settings file path.</summary>
    public string DefaultsFilePath { get; }

    /// <summary>Initializes a new instance of the <see cref="UserSettingsFile"/> class.</summary>
    /// <param name="userSettingsPath">Optional user settings file path override.</param>
    /// <param name="defaultsFilePath">Optional defaults file path override.</param>
    public UserSettingsFile(string? userSettingsPath = null, string? defaultsFilePath = null)
    {

        UserSettingsPath = string.IsNullOrWhiteSpace(userSettingsPath) ? DefaultUserSettingsPath : userSettingsPath;
        DefaultsFilePath = string.IsNullOrWhiteSpace(defaultsFilePath) ? Path.Combine(AppContext.BaseDirectory, DEFAULT_CONFIG_FILE) : defaultsFilePath;
    }

    /// <summary>Loads the effective user configuration, falling back to defaults for missing sections.</summary>
    /// <returns>The resolved settings instance.</returns>
    public UserSettings Load()
    {

        var user = ReadRaw(UserSettingsPath);
        var defaults = ReadRaw(DefaultsFilePath);

        if (user is null && !File.Exists(UserSettingsPath) && HasCustomizations(defaults))
        {

            var migrated = MergeWithDefaults(null, defaults);
            Save(migrated);

            return migrated;
        }

        return MergeWithDefaults(user, defaults);
    }

    /// <summary>Asynchronously loads the effective user configuration, falling back to defaults for missing sections.</summary>
    /// <param name="cancellationToken">Token cancelling the read operation.</param>
    /// <returns>The resolved settings instance.</returns>
    public async Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default)
    {

        var user = await ReadRawAsync(UserSettingsPath, cancellationToken).ConfigureAwait(false);
        var defaults = await ReadRawAsync(DefaultsFilePath, cancellationToken).ConfigureAwait(false);

        if (user is null && !File.Exists(UserSettingsPath) && HasCustomizations(defaults))
        {

            var migrated = MergeWithDefaults(null, defaults);
            await SaveAsync(migrated, cancellationToken).ConfigureAwait(false);

            return migrated;
        }

        return MergeWithDefaults(user, defaults);
    }

    /// <summary>Atomically persists user settings to disk.</summary>
    /// <param name="settings">The settings to serialize and save.</param>
    /// <returns><see langword="true"/> when written successfully; otherwise <see langword="false"/>.</returns>
    public bool Save(UserSettings settings)
    {

        ArgumentNullException.ThrowIfNull(settings);

        using var gate = SettingsFileGate.Acquire(UserSettingsPath);

        return WriteFileAtomic(settings);
    }

    /// <summary>Asynchronously and atomically persists user settings to disk.</summary>
    /// <param name="settings">The settings to serialize and save.</param>
    /// <param name="cancellationToken">Token cancelling the save operation.</param>
    /// <returns><see langword="true"/> when written successfully; otherwise <see langword="false"/>.</returns>
    public async Task<bool> SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(settings);

        using var gate = await SettingsFileGate.AcquireAsync(UserSettingsPath, cancellationToken).ConfigureAwait(false);

        return await WriteFileAtomicAsync(settings, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Atomically mutates the user settings file under the file gate.</summary>
    /// <param name="updateAction">Transformation applied to the existing or default settings.</param>
    /// <returns><see langword="true"/> when saved successfully; otherwise <see langword="false"/>.</returns>
    public bool Update(Func<UserSettings, UserSettings> updateAction)
    {

        ArgumentNullException.ThrowIfNull(updateAction);

        using var gate = SettingsFileGate.Acquire(UserSettingsPath);

        var raw = ReadRaw(UserSettingsPath);
        var defaults = raw is null ? ReadRaw(DefaultsFilePath) : null;
        var current = raw ?? (HasCustomizations(defaults) ? MergeWithDefaults(null, defaults) : new UserSettings());

        return WriteFileAtomic(updateAction(current));
    }

    /// <summary>Asynchronously and atomically mutates the user settings file under the file gate.</summary>
    /// <param name="updateAction">Transformation applied to the existing or default settings.</param>
    /// <param name="cancellationToken">Token cancelling the update operation.</param>
    /// <returns><see langword="true"/> when saved successfully; otherwise <see langword="false"/>.</returns>
    public async Task<bool> UpdateAsync(Func<UserSettings, UserSettings> updateAction, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(updateAction);

        using var gate = await SettingsFileGate.AcquireAsync(UserSettingsPath, cancellationToken).ConfigureAwait(false);

        var raw = await ReadRawAsync(UserSettingsPath, cancellationToken).ConfigureAwait(false);
        var defaults = raw is null ? await ReadRawAsync(DefaultsFilePath, cancellationToken).ConfigureAwait(false) : null;
        var current = raw ?? (HasCustomizations(defaults) ? MergeWithDefaults(null, defaults) : new UserSettings());

        return await WriteFileAtomicAsync(updateAction(current), cancellationToken).ConfigureAwait(false);
    }

    private static bool HasCustomizations(UserSettings? defaults)
        => defaults is not null
           && (defaults.Hud?.Left is not null
               || defaults.Hud?.Top is not null
               || HasCustomProviders(defaults.Providers)
               || HasCustomRefresh(defaults.Refresh)
               || (defaults.RateLimit?.MinimumRetryFloorSeconds is { } floor && floor != RateLimitSettings.DEFAULT_FLOOR_SECONDS));

    private static bool HasCustomProviders(ProviderSettings? providers)
        => providers?.EnabledStates.Values.Any(static isEnabled => !isEnabled) == true
           || providers?.EnabledStates.Keys.Any(static key => !DEFAULT_PROVIDERS.Contains(key)) == true;

    private static bool HasCustomRefresh(RefreshSettings? refresh)
        => (refresh?.ActiveIntervalSeconds is { } active && active != 180)
           || (refresh?.IdleIntervalSeconds is { } idle && idle != 300);

    private static UserSettings MergeWithDefaults(UserSettings? user, UserSettings? defaults)
        => new()
        {
            Hud = user?.Hud ?? defaults?.Hud ?? new HudPositionSettings(),
            Providers = user?.Providers ?? defaults?.Providers ?? new ProviderSettings(),
            Refresh = user?.Refresh ?? defaults?.Refresh ?? new RefreshSettings(),
            RateLimit = user?.RateLimit ?? defaults?.RateLimit ?? new RateLimitSettings(),
            ExtensionData = user?.ExtensionData ?? defaults?.ExtensionData
        };

    private static UserSettings? ReadRaw(string filePath)
    {

        if (!File.Exists(filePath))
            return null;

        try
        {

            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllBytes(filePath), JSON_OPTIONS);
        }
        catch (Exception ex)
        {

            Log.Warning(ex, "Failed to deserialize settings from {FilePath}", filePath);

            return null;
        }
    }

    private static async Task<UserSettings?> ReadRawAsync(string filePath, CancellationToken cancellationToken)
    {

        if (!File.Exists(filePath))
            return null;

        try
        {

            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<UserSettings>(bytes, JSON_OPTIONS);
        }
        catch (Exception ex)
        {

            Log.Warning(ex, "Failed to deserialize settings from {FilePath}", filePath);

            return null;
        }
    }

    private static void TryDeleteFile(string filePath)
    {

        try
        {

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
        }
    }

    private void EnsureDirectory()
    {

        if (Path.GetDirectoryName(UserSettingsPath) is { } directory && directory.Length > 0)
            Directory.CreateDirectory(directory);
    }

    private bool WriteFileAtomic(UserSettings settings)
    {

        EnsureDirectory();

        var tempPath = $"{UserSettingsPath}.tmp";

        try
        {

            File.WriteAllBytes(tempPath, JsonSerializer.SerializeToUtf8Bytes(settings, JSON_OPTIONS));
            File.Move(tempPath, UserSettingsPath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {

            TryDeleteFile(tempPath);
            Log.Warning(ex, "Failed to persist user settings to {UserSettingsPath}", UserSettingsPath);

            return false;
        }
    }

    private async Task<bool> WriteFileAtomicAsync(UserSettings settings, CancellationToken cancellationToken)
    {

        EnsureDirectory();

        var tempPath = $"{UserSettingsPath}.tmp";

        try
        {

            var bytes = JsonSerializer.SerializeToUtf8Bytes(settings, JSON_OPTIONS);

            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, UserSettingsPath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {

            TryDeleteFile(tempPath);
            Log.Warning(ex, "Failed to persist user settings to {UserSettingsPath}", UserSettingsPath);

            return false;
        }
    }
}
