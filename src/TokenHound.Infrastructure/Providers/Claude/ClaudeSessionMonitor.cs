using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Storage;
using TokenHound.Infrastructure.System;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Monitors local Claude Code CLI activity by scanning session files and validating process liveness.
/// </summary>
public sealed class ClaudeSessionMonitor : IActivityMonitor
{
    /// <summary>
    /// The unique provider identifier for Claude Code.
    /// </summary>
    public const string PROVIDER_ID = "claude";

    private const string PID_PROPERTY_NAME = "pid";
    private const string SESSIONS_SEARCH_PATTERN = "*.json";

    private static readonly TimeSpan DEFAULT_START_TIME_TOLERANCE = TimeSpan.FromMinutes(5);
    private static readonly string[] STARTED_AT_PROPERTIES = ["startedAt", "started_at", "startTime"];
    private static readonly string[] UPDATED_AT_PROPERTIES = ["statusUpdatedAt", "status_updated_at", "updatedAt"];

    private readonly string _baseDirectory;
    private readonly TimeSpan _startTimeTolerance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeSessionMonitor"/> class.
    /// </summary>
    /// <param name="baseDirectory">The base directory containing the .claude directory, or <see langword="null"/> to use the user profile.</param>
    /// <param name="startTimeTolerance">The tolerance threshold for matching process start time, or <see langword="null"/> to use the default 5 minutes.</param>
    public ClaudeSessionMonitor(
        string? baseDirectory = null,
        TimeSpan? startTimeTolerance = null)
    {

        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;

        _startTimeTolerance = startTimeTolerance ?? DEFAULT_START_TIME_TOLERANCE;
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Gets the base directory containing Claude profiles.
    /// </summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>
    /// Gets the resolved path to the Claude sessions directory.
    /// </summary>
    public string SessionsDirectory
        => Path.Combine(_baseDirectory, ".claude", "sessions");

    /// <inheritdoc />
    public async ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var sessionsDir = SessionsDirectory;

        if (!Directory.Exists(sessionsDir))
            return null;

        string[] files;

        try
        {

            files = Directory.GetFiles(sessionsDir, SESSIONS_SEARCH_PATTERN);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return null;
        }

        if (files.Length == 0)
            return null;

        return await ScanSessionsAsync(files, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses a Claude Code session JSON string into a structured session descriptor.
    /// </summary>
    /// <param name="jsonContent">The raw session JSON string.</param>
    /// <returns>The parsed <see cref="SessionFileInfo"/>, or <see langword="null"/> if invalid.</returns>
    public static SessionFileInfo? ParseSessionJson(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            return ExtractSessionInfo(root);
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private async ValueTask<AgentSession?> ScanSessionsAsync(
        string[] sessionFiles,
        CancellationToken cancellationToken)
    {

        foreach (var file in sessionFiles)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var session = await ProcessSessionFileAsync(file, cancellationToken).ConfigureAwait(false);

            if (session is not null)
                return session;
        }

        return null;
    }

    private async ValueTask<AgentSession?> ProcessSessionFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {

        var json = await SharedFileReader.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var parsed = ParseSessionJson(json);

        if (parsed is null)
            return null;

        if (!ProcessLiveness.IsProcessAlive(parsed.Pid, parsed.StartedAtUtc, _startTimeTolerance))
            return null;

        return CreateActiveSession(parsed);
    }

    private static AgentSession CreateActiveSession(SessionFileInfo parsed)
    {

        var startUtc = parsed.StartedAtUtc
            ?? ProcessLiveness.GetProcessStartTimeUtc(parsed.Pid)
            ?? DateTimeOffset.UtcNow;

        return new AgentSession
        {
            Pid = parsed.Pid,
            StartTimeUtc = startUtc,
            State = AgentSessionState.Busy,
            LastActivityUtc = parsed.StatusUpdatedAtUtc ?? startUtc
        };
    }

    private static SessionFileInfo? ExtractSessionInfo(JsonElement root)
    {

        var pid = ExtractPid(root);

        if (pid is null || pid.Value <= 0)
            return null;

        return new SessionFileInfo(
            pid.Value,
            ExtractTimestamp(root, STARTED_AT_PROPERTIES),
            ExtractTimestamp(root, UPDATED_AT_PROPERTIES));
    }

    private static int? ExtractPid(JsonElement root)
    {

        if (!TryGetPropertyIgnoreCase(root, PID_PROPERTY_NAME, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var pidVal))
            return pidVal;

        if (prop.ValueKind == JsonValueKind.String &&
            int.TryParse(prop.GetString(), CultureInfo.InvariantCulture, out var parsedPid))
            return parsedPid;

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {

        foreach (var property in element.EnumerateObject())
        {

            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {

                value = property.Value;

                return true;
            }
        }

        value = default;

        return false;
    }

    private static DateTimeOffset? ExtractTimestamp(JsonElement root, string[] propertyNames)
    {

        foreach (var name in propertyNames)
        {

            if (TryGetPropertyIgnoreCase(root, name, out var prop))
            {

                var parsed = ParseTimestampElement(prop);

                if (parsed.HasValue)
                    return parsed;
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseTimestampElement(JsonElement element)
    {

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var epochVal))
            return ParseEpochTimestamp(epochVal);

        if (element.ValueKind != JsonValueKind.String)
            return null;

        var str = element.GetString();

        if (long.TryParse(str, CultureInfo.InvariantCulture, out var parsedEpoch))
            return ParseEpochTimestamp(parsedEpoch);

        if (DateTimeOffset.TryParse(
            str,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsedDate))
            return parsedDate;

        return null;
    }

    private static DateTimeOffset? ParseEpochTimestamp(long epochValue)
    {

        try
        {

            return epochValue > 10_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
                : DateTimeOffset.FromUnixTimeSeconds(epochValue);
        }
        catch (ArgumentOutOfRangeException)
        {

            return null;
        }
    }

    /// <summary>
    /// Represents extracted session descriptor fields from a session file.
    /// </summary>
    public sealed record SessionFileInfo(
        int Pid,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? StatusUpdatedAtUtc);
}
