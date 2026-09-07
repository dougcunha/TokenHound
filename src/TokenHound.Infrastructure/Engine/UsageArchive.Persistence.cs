using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    private Dictionary<string, Snapshot> ReadReadings(ICollection<string> errors)
    {

        if (!File.Exists(LastReadingsPath))
            return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(LastReadingsPath);
            var readings = JsonSerializer.Deserialize<Dictionary<string, Snapshot>>(json, SERIALIZER_OPTIONS);

            return readings is null
                ? new(StringComparer.OrdinalIgnoreCase)
                : new(readings, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (IsPersistenceException(ex))
        {
            errors.Add($"{LAST_READINGS_FILE_NAME}: {ex.Message}");

            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, DateTimeOffset> ReadDeadlines(ICollection<string> errors)
    {

        if (!File.Exists(StatePath))
            return new(StringComparer.OrdinalIgnoreCase);

        try
        {
            var state = ParseStateObject(File.ReadAllText(StatePath));
            var deadlines = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

            if (state[BACKOFF_UNTIL_PROPERTY_NAME] is null)
                return deadlines;

            if (state[BACKOFF_UNTIL_PROPERTY_NAME] is not JsonObject backoffUntil)
                throw new InvalidDataException("The backoffUntil archive value must be a JSON object.");

            foreach (var entry in backoffUntil)
            {

                var rawDeadline = entry.Value is JsonValue jsonValue
                    && jsonValue.TryGetValue<string>(out var stringDeadline)
                    ? stringDeadline
                    : null;

                if (DateTimeOffset.TryParse(
                    rawDeadline,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var deadline))
                {
                    deadlines[entry.Key] = deadline;
                }
                else
                {
                    errors.Add($"{STATE_FILE_NAME}: invalid deadline for {entry.Key}");
                }
            }

            return deadlines;
        }
        catch (Exception ex) when (IsPersistenceException(ex))
        {
            errors.Add($"{STATE_FILE_NAME}: {ex.Message}");

            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, Snapshot> ReadReadingsForWrite()
    {

        if (!File.Exists(LastReadingsPath))
            return new(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(LastReadingsPath);
        var readings = JsonSerializer.Deserialize<Dictionary<string, Snapshot>>(json, SERIALIZER_OPTIONS)
            ?? new Dictionary<string, Snapshot>();

        return new(readings, StringComparer.OrdinalIgnoreCase);
    }

    private JsonObject ReadStateObjectForWrite()
    {

        if (!File.Exists(StatePath))
            return [];

        return ParseStateObject(File.ReadAllText(StatePath));
    }

    private static JsonObject ParseStateObject(string json)
        => JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidDataException("The archive state root must be a JSON object.");

    private static JsonObject GetBackoffObjectForWrite(JsonObject state)
    {

        if (state[BACKOFF_UNTIL_PROPERTY_NAME] is null)
        {
            var backoffUntil = new JsonObject();
            state[BACKOFF_UNTIL_PROPERTY_NAME] = backoffUntil;

            return backoffUntil;
        }

        return state[BACKOFF_UNTIL_PROPERTY_NAME] as JsonObject
            ?? throw new InvalidDataException("The backoffUntil archive value must be a JSON object.");
    }

    private async Task WriteJsonAtomicallyAsync(
        string path,
        object value,
        CancellationToken cancellationToken)
    {

        Directory.CreateDirectory(DirectoryPath);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            var json = JsonSerializer.Serialize(value, SERIALIZER_OPTIONS);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            ReplaceFile(temporaryPath, path);
        }
        finally
        {

            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void ReplaceFile(string temporaryPath, string destinationPath)
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
                // The destination was removed concurrently; Move below recreates it.
            }
        }

        File.Move(temporaryPath, destinationPath, true);
    }

    private static bool IsPersistenceException(Exception exception)
        => exception is JsonException
            or NotSupportedException
            or IOException
            or UnauthorizedAccessException
            or InvalidDataException;

    /// <summary>
    /// Represents the state loaded from both archive files.
    /// </summary>
    public sealed record State
    {
        /// <summary>
        /// Gets the archived successful snapshots indexed by provider identifier.
        /// </summary>
        public required IReadOnlyDictionary<string, Snapshot> LastReadings { get; init; }

        /// <summary>
        /// Gets absolute provider backoff deadlines indexed by provider identifier.
        /// </summary>
        public required IReadOnlyDictionary<string, DateTimeOffset> BackoffDeadlines { get; init; }

        /// <summary>
        /// Gets a local archive diagnostic, or null when both files were read successfully.
        /// </summary>
        public string? ErrorDescription { get; init; }
    }
}
