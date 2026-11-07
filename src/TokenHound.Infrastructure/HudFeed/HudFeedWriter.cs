using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.HudFeed;

/// <summary>Writes the HUD feed document atomically to <c>%LOCALAPPDATA%\TokenHound\hud.json</c>.</summary>
public sealed class HudFeedWriter
{
    /// <summary>The file name of the HUD feed.</summary>
    public const string FILE_NAME = "hud.json";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly string _directoryPath;

    /// <summary>Creates a writer for a directory, or for the TokenHound local data directory when null.</summary>
    /// <param name="directoryPath">The target directory, or null for the default.</param>
    public HudFeedWriter(string? directoryPath = null)
    {

        _directoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TokenHound"
        );
        FilePath = Path.Combine(_directoryPath, FILE_NAME);
    }

    /// <summary>Gets the full path of the feed file.</summary>
    public string FilePath { get; }

    /// <summary>Replaces the feed file with the document; readers never see a partial file.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the file is replaced.</returns>
    public Task WriteAsync(HudFeedDocument document, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(document);

        return AtomicJsonFile.WriteAsync(
            _directoryPath,
            FilePath,
            document,
            SERIALIZER_OPTIONS,
            cancellationToken
        );
    }
}
