using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

internal static class AtomicJsonFile
{
    internal static async Task WriteAsync(
        string directoryPath,
        string path,
        object value,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
    {

        Directory.CreateDirectory(directoryPath);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            var json = JsonSerializer.Serialize(value, serializerOptions);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            Replace(temporaryPath, path);
        }
        finally
        {

            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void Replace(string temporaryPath, string destinationPath)
    {

        if (File.Exists(destinationPath))
        {
            try
            {
                File.Replace(temporaryPath, destinationPath, null, true);

                return;
            }
            catch (FileNotFoundException)
            {
                // The destination was removed after the existence check; Move below recreates it.
            }
        }

        File.Move(temporaryPath, destinationPath, true);
    }
}
