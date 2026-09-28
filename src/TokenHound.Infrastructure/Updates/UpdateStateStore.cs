using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Reads and atomically writes <c>update-state.json</c> in the TokenHound local application data directory.
/// </summary>
public sealed class UpdateStateStore
{
    /// <summary>
    /// The state file name.
    /// </summary>
    public const string FILE_NAME = "update-state.json";

    private const string LAST_CHECK_PROPERTY_NAME = "lastCheckUtc";
    private const string DEADLINE_PROPERTY_NAME = "deadlineUtc";
    private const string FAILURES_PROPERTY_NAME = "consecutiveFailures";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { WriteIndented = true };

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStateStore"/> class.
    /// </summary>
    /// <param name="directoryPath">An optional directory override; defaults to <c>%LOCALAPPDATA%\TokenHound</c>.</param>
    public UpdateStateStore(string? directoryPath = null)
    {

        _directoryPath = string.IsNullOrWhiteSpace(directoryPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TokenHound")
            : directoryPath;

        FilePath = Path.Combine(_directoryPath, FILE_NAME);
    }

    /// <summary>
    /// Gets the full path of the state file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Loads the persisted state; a missing or unreadable file yields an empty state, and each field is read independently.
    /// </summary>
    /// <returns>The persisted state, keeping every field that parses.</returns>
    public UpdateState Load()
    {

        if (!File.Exists(FilePath))
            return new UpdateState();

        try
        {

            return JsonNode.Parse(File.ReadAllText(FilePath)) is JsonObject root ? Parse(root) : new UpdateState();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {

            return new UpdateState();
        }
    }

    /// <summary>
    /// Atomically replaces the persisted state.
    /// </summary>
    /// <param name="state">The state to write.</param>
    /// <param name="cancellationToken">Token cancelling the write.</param>
    /// <returns>A task completing when the file was replaced; faults when the write fails.</returns>
    public Task SaveAsync(UpdateState state, CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(state);

        var root = new JsonObject
        {
            [LAST_CHECK_PROPERTY_NAME] = Format(state.LastCheckUtc),
            [DEADLINE_PROPERTY_NAME] = Format(state.DeadlineUtc),
            [FAILURES_PROPERTY_NAME] = Math.Max(0, state.ConsecutiveFailures)
        };

        return AtomicJsonFile.WriteAsync(
            _directoryPath,
            FilePath,
            root,
            SERIALIZER_OPTIONS,
            cancellationToken
        );
    }

    private static UpdateState Parse(JsonObject root)
        => new()
        {
            LastCheckUtc = ReadInstant(root, LAST_CHECK_PROPERTY_NAME),
            DeadlineUtc = ReadInstant(root, DEADLINE_PROPERTY_NAME),
            ConsecutiveFailures = root[FAILURES_PROPERTY_NAME] is JsonValue value && value.TryGetValue<int>(out var failures)
                ? Math.Max(0, failures)
                : 0
        };

    private static DateTimeOffset? ReadInstant(JsonObject root, string propertyName)
    {

        if (root[propertyName] is not JsonValue value || !value.TryGetValue<string>(out var text))
            return null;

        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var instant
        ) ? instant : null;
    }

    private static string? Format(DateTimeOffset? instant)
        => instant?.ToString("O", CultureInfo.InvariantCulture);
}
