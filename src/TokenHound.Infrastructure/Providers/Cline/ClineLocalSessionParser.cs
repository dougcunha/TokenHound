using System;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Parses a Cline session message artifact into locally derived usage evidence.
/// </summary>
/// <remarks>
/// Each assistant message carries a <c>metrics</c> object with input, output, and cache token buckets,
/// plus a millisecond <c>ts</c> timestamp. The reader never writes to the artifact.
/// </remarks>
public static class ClineLocalSessionParser
{
    private const string CACHE_READ_TOKENS_PROPERTY = "cacheReadTokens";
    private const string CACHE_WRITE_TOKENS_PROPERTY = "cacheWriteTokens";
    private const string INPUT_TOKENS_PROPERTY = "inputTokens";
    private const string MESSAGES_PROPERTY = "messages";
    private const string METRICS_PROPERTY = "metrics";
    private const string OUTPUT_TOKENS_PROPERTY = "outputTokens";
    private const string TIMESTAMP_PROPERTY = "ts";

    /// <summary>
    /// Parses one session message artifact and aggregates the token metrics recorded after the window start.
    /// </summary>
    /// <param name="jsonContent">The raw session message JSON.</param>
    /// <param name="windowStartUtc">The inclusive UTC start of the aggregation window.</param>
    /// <returns>The sampled usage and the newest recorded free model limit failure, when present.</returns>
    public static ClineLocalUsageSample ParseMessages(
        string? jsonContent,
        DateTimeOffset windowStartUtc)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return ClineLocalUsageSample.Empty;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);

            if (!document.RootElement.TryGetProperty(MESSAGES_PROPERTY, out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
                return ClineLocalUsageSample.Empty;

            var accumulator = new TokenAccumulator();
            ClineFreeLimitHit? freeLimit = null;

            foreach (var message in messages.EnumerateArray())
            {

                var timestampUtc = ReadTimestamp(message);

                AddMetrics(accumulator, message, timestampUtc, windowStartUtc);

                var hit = ReadFreeLimit(message, timestampUtc);

                if (hit is not null && (freeLimit is null || hit.DetectedAtUtc > freeLimit.DetectedAtUtc))
                    freeLimit = hit;
            }

            return new ClineLocalUsageSample
            {
                Usage = accumulator.ToUsage(windowStartUtc),
                FreeLimit = freeLimit
            };
        }
        catch (JsonException)
        {

            return ClineLocalUsageSample.Empty;
        }
    }

    private static void AddMetrics(
        TokenAccumulator accumulator,
        JsonElement message,
        DateTimeOffset? timestampUtc,
        DateTimeOffset windowStartUtc)
    {

        if (timestampUtc is not { } recordedAtUtc || recordedAtUtc < windowStartUtc)
            return;

        if (!message.TryGetProperty(METRICS_PROPERTY, out var metrics) || metrics.ValueKind != JsonValueKind.Object)
            return;

        accumulator.Add(
            ReadLong(metrics, INPUT_TOKENS_PROPERTY),
            ReadLong(metrics, OUTPUT_TOKENS_PROPERTY),
            ReadLong(metrics, CACHE_READ_TOKENS_PROPERTY),
            ReadLong(metrics, CACHE_WRITE_TOKENS_PROPERTY),
            recordedAtUtc
        );
    }

    private static ClineFreeLimitHit? ReadFreeLimit(JsonElement message, DateTimeOffset? timestampUtc)
    {

        var rawText = message.GetRawText();

        if (!ClineFreeModelLimit.IsLimitMessage(rawText))
            return null;

        var retryMarkerIndex = rawText.IndexOf(
            ClineFreeModelLimit.RETRY_MARKER,
            StringComparison.OrdinalIgnoreCase
        );

        return new ClineFreeLimitHit
        {
            DetectedAtUtc = timestampUtc ?? DateTimeOffset.MinValue,
            RetryAfter = ClineFreeModelLimit.ParseRetryDelay(rawText),
            Message = retryMarkerIndex >= 0
                ? rawText.Substring(0, Math.Min(rawText.Length, retryMarkerIndex))
                : rawText
        };
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement message)
    {

        if (!message.TryGetProperty(TIMESTAMP_PROPERTY, out var timestamp))
            return null;

        if (timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var epochMilliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds);

        if (timestamp.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(timestamp.GetString(), out var parsed))
            return parsed.ToUniversalTime();

        return null;
    }

    private static long ReadLong(JsonElement parent, string propertyName)
    {

        if (!parent.TryGetProperty(propertyName, out var value))
            return 0L;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number;

        return 0L;
    }

    private sealed class TokenAccumulator
    {
        private long _cacheReadTokens;
        private long _cacheWriteTokens;
        private long _inputTokens;
        private DateTimeOffset _lastActivityUtc = DateTimeOffset.MinValue;
        private int _modelCalls;
        private long _outputTokens;

        public void Add(
            long inputTokens,
            long outputTokens,
            long cacheReadTokens,
            long cacheWriteTokens,
            DateTimeOffset recordedAtUtc)
        {

            _inputTokens += inputTokens;
            _outputTokens += outputTokens;
            _cacheReadTokens += cacheReadTokens;
            _cacheWriteTokens += cacheWriteTokens;
            _modelCalls++;

            if (recordedAtUtc > _lastActivityUtc)
                _lastActivityUtc = recordedAtUtc;
        }

        public ClineLocalUsage? ToUsage(DateTimeOffset windowStartUtc)
        {

            if (_modelCalls == 0)
                return null;

            return new ClineLocalUsage
            {
                InputTokens = _inputTokens,
                OutputTokens = _outputTokens,
                CacheReadTokens = _cacheReadTokens,
                CacheWriteTokens = _cacheWriteTokens,
                ModelCalls = _modelCalls,
                WindowStartUtc = windowStartUtc,
                LastActivityUtc = _lastActivityUtc
            };
        }
    }
}