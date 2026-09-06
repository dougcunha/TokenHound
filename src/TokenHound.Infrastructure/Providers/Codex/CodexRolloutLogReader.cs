using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>Reads Codex rate limits from active rollout logs indexed by state_5.sqlite.</summary>
public sealed class CodexRolloutLogReader
{
    private const int MAX_TAIL_BYTES = 262144;
    private const string CODEX_DIRECTORY_NAME = ".codex";
    private const string DATABASE_FILE_NAME = "state_5.sqlite";
    private const string ACTIVE_ROLLOUTS_QUERY = "SELECT rollout_path FROM threads WHERE archived = 0 ORDER BY updated_at_ms DESC LIMIT 8;";
    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { PropertyNameCaseInsensitive = true };

    private readonly string _baseDirectory;

    /// <summary>Initializes a reader using the current user's profile directory.</summary>
    /// <param name="baseDirectory">The user profile directory, or null for the current profile.</param>
    public CodexRolloutLogReader(string? baseDirectory = null)
    {

        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;
    }

    /// <summary>Gets the user profile directory used for rollout discovery.</summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>Gets the state database path used for active rollout discovery.</summary>
    public string DatabasePath
        => Path.Combine(_baseDirectory, Path.Combine(CODEX_DIRECTORY_NAME, DATABASE_FILE_NAME));

    /// <summary>Reads the newest available rate limits from active rollout logs.</summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The newest rate limits, or null when no usable reading exists.</returns>
    public async Task<CodexRateLimitsDto?> ReadLatestRateLimitsAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var paths = await ReadActiveRolloutPathsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var path in paths)
        {

            var rateLimits = await ReadRateLimitsFromFileAsync(path, cancellationToken).ConfigureAwait(false);

            if (rateLimits is not null)
                return rateLimits;
        }

        return null;
    }

    /// <summary>Reads the final 256 KB of one rollout file for its newest rate limits event.</summary>
    /// <param name="filePath">The rollout JSONL file path.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The newest rate limits, or null when the file is missing or has no reading.</returns>
    public static async Task<CodexRateLimitsDto?> ReadRateLimitsFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {

            using var stream = SharedFileReader.OpenRead(filePath);
            var start = Math.Max(0, stream.Length - MAX_TAIL_BYTES);
            stream.Seek(start, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var tail = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            return FindLatestRateLimits(tail);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            return null;
        }
    }

    private async Task<IReadOnlyList<string>> ReadActiveRolloutPathsAsync(CancellationToken cancellationToken)
    {

        try
        {

            return await SafeSqliteReader.QueryAsync(
                DatabasePath,
                ACTIVE_ROLLOUTS_QUERY,
                static reader => reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            return [];
        }
    }

    private static CodexRateLimitsDto? FindLatestRateLimits(string tail)
    {

        var end = tail.Length;

        while (end > 0)
        {

            var newline = tail.LastIndexOf('\n', end - 1);
            var start = newline + 1;
            var rateLimits = ParseLine(tail.AsSpan(start, end - start));

            if (rateLimits is not null)
                return rateLimits;

            end = newline;
        }

        return null;
    }

    private static CodexRateLimitsDto? ParseLine(ReadOnlySpan<char> line)
    {

        try
        {

            var rolloutEvent = JsonSerializer.Deserialize<RolloutEvent>(line, SERIALIZER_OPTIONS);
            var rateLimits = rolloutEvent?.Payload?.RateLimits;

            if (!string.Equals(rolloutEvent?.Type, "event_msg", StringComparison.Ordinal) ||
                !string.Equals(rolloutEvent?.Payload?.Type, "token_count", StringComparison.Ordinal) ||
                rateLimits is null)
                return null;

            return new CodexRateLimitsDto
            {
                PlanType = rateLimits.PlanType,
                RateLimitReachedType = rateLimits.RateLimitReachedType,
                Primary = MapWindow(rateLimits.Primary),
                Secondary = MapWindow(rateLimits.Secondary)
            };
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static CodexRateLimitsDto.RateLimitWindow? MapWindow(RolloutWindow? window)
        => window is null
            ? null
            : new CodexRateLimitsDto.RateLimitWindow
            {
                UsedPercent = window.UsedPercent,
                WindowDurationMins = window.WindowMinutes,
                ResetsAt = window.ResetsAt
            };

    private sealed record RolloutEvent(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("payload")] RolloutPayload? Payload);

    private sealed record RolloutPayload(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("rate_limits")] RolloutRateLimits? RateLimits);

    private sealed record RolloutRateLimits(
        [property: JsonPropertyName("plan_type")] string? PlanType,
        [property: JsonPropertyName("rate_limit_reached_type")] string? RateLimitReachedType,
        [property: JsonPropertyName("primary")] RolloutWindow? Primary,
        [property: JsonPropertyName("secondary")] RolloutWindow? Secondary);

    private sealed record RolloutWindow(
        [property: JsonPropertyName("used_percent")] double? UsedPercent,
        [property: JsonPropertyName("window_minutes")] int? WindowMinutes,
        [property: JsonPropertyName("resets_at")] long? ResetsAt);
}
