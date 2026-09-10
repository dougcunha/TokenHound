using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

public sealed partial class UserSettingsFile
{
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

    private static async Task<UserSettings?> ReadRawAsync(
        string filePath,
        CancellationToken cancellationToken)
    {

        if (!File.Exists(filePath))
            return null;

        try
        {

            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<UserSettings>(bytes, JSON_OPTIONS);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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

    private string PrepareTempPath()
    {

        EnsureDirectory();

        return $"{UserSettingsPath}.tmp";
    }

    private static byte[] SerializeSettings(UserSettings settings)
        => JsonSerializer.SerializeToUtf8Bytes(settings, JSON_OPTIONS);

    private void HandleWriteFailure(string tempPath, Exception exception)
    {

        TryDeleteFile(tempPath);
        Log.Warning(exception, "Failed to persist user settings to {UserSettingsPath}", UserSettingsPath);
    }

    private bool WriteFileAtomic(UserSettings settings)
    {

        var tempPath = PrepareTempPath();

        try
        {

            File.WriteAllBytes(tempPath, SerializeSettings(settings));
            File.Move(tempPath, UserSettingsPath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {

            HandleWriteFailure(tempPath, ex);

            return false;
        }
    }

    private async Task<bool> WriteFileAtomicAsync(
        UserSettings settings,
        CancellationToken cancellationToken)
    {

        var tempPath = PrepareTempPath();

        try
        {

            await File.WriteAllBytesAsync(tempPath, SerializeSettings(settings), cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, UserSettingsPath, overwrite: true);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDeleteFile(tempPath);

            throw;
        }
        catch (Exception ex)
        {

            HandleWriteFailure(tempPath, ex);

            return false;
        }
    }
}
