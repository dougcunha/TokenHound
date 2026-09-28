using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Stores the list of files a portable update swapped, so the relaunched executable can delete the previous
/// <c>.old</c> copies once the old process has exited.
/// </summary>
public sealed class UpdateSwapJournal
{
    /// <summary>
    /// The journal file name.
    /// </summary>
    public const string FILE_NAME = "swap-journal.json";

    /// <summary>
    /// The suffix given to replaced files until the new version has started.
    /// </summary>
    public const string OLD_SUFFIX = ".old";

    private static readonly ILogger LOGGER = Log.ForContext<UpdateSwapJournal>();
    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { WriteIndented = true };

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSwapJournal"/> class.
    /// </summary>
    /// <param name="updatesDirectory">An optional directory override; defaults to <c>%LOCALAPPDATA%\TokenHound\updates</c>.</param>
    public UpdateSwapJournal(string? updatesDirectory = null)
    {

        _directoryPath = string.IsNullOrWhiteSpace(updatesDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TokenHound", "updates")
            : updatesDirectory;

        FilePath = Path.Combine(_directoryPath, FILE_NAME);
    }

    /// <summary>
    /// Gets the full path of the journal file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Writes the journal, replacing any previous one.
    /// </summary>
    /// <param name="entry">The swap to record.</param>
    public void Write(SwapJournalEntry entry)
    {

        ArgumentNullException.ThrowIfNull(entry);

        Directory.CreateDirectory(_directoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entry, SERIALIZER_OPTIONS));
    }

    /// <summary>
    /// Reads the journal.
    /// </summary>
    /// <returns>The recorded swap, or <see langword="null"/> when there is none or it cannot be read.</returns>
    public SwapJournalEntry? Read()
    {

        if (!File.Exists(FilePath))
            return null;

        try
        {

            return JsonSerializer.Deserialize<SwapJournalEntry>(File.ReadAllText(FilePath), SERIALIZER_OPTIONS);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {

            LOGGER.Warning(ex, "Unreadable swap journal {Path}", FilePath);

            return null;
        }
    }

    /// <summary>
    /// Deletes the journal file when present.
    /// </summary>
    public void Delete()
    {

        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    /// <summary>
    /// Deletes the journaled <c>.old</c> files and then the journal. Idempotent; a file still locked keeps the journal for the next start.
    /// </summary>
    /// <returns>The number of <c>.old</c> files deleted.</returns>
    public int CleanupAfterUpdate()
    {

        var entry = Read();

        if (entry is null)
            return 0;

        var (deleted, pending) = DeleteOldFiles(entry);

        if (pending == 0)
            Delete();

        LOGGER.Information("UpdateCleanupCompleted {Files}", deleted);

        return deleted;
    }

    private static (int Deleted, int Pending) DeleteOldFiles(SwapJournalEntry entry)
    {

        var deleted = 0;
        var pending = 0;

        foreach (var relativePath in entry.Files)
        {
            var oldPath = Path.Combine(entry.AppDirectory, relativePath) + OLD_SUFFIX;

            if (!File.Exists(oldPath))
                continue;

            if (TryDelete(oldPath))
                deleted++;
            else
                pending++;
        }

        return (deleted, pending);
    }

    private static bool TryDelete(string path)
    {
        try
        {

            File.Delete(path);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            LOGGER.Warning(ex, "Could not delete previous version file {Path}", path);

            return false;
        }
    }

    /// <summary>
    /// Builds the relative paths recorded for the files under <paramref name="stagingDirectory"/>.
    /// </summary>
    /// <param name="stagingDirectory">The folder holding the extracted update.</param>
    /// <returns>Relative paths of every staged file.</returns>
    public static IReadOnlyList<string> ListRelativeFiles(string stagingDirectory)
    {

        var files = new List<string>();

        foreach (var path in Directory.EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories))
            files.Add(Path.GetRelativePath(stagingDirectory, path));

        return files;
    }
}
