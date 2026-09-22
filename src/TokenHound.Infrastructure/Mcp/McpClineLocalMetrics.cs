using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains Cline usage aggregated from local session evidence.</summary>
public sealed record McpClineLocalMetrics
{
    /// <summary>Gets input token use.</summary>
    [JsonPropertyName("inputTokens")]
    public required long InputTokens { get; init; }

    /// <summary>Gets output token use.</summary>
    [JsonPropertyName("outputTokens")]
    public required long OutputTokens { get; init; }

    /// <summary>Gets prompt-cache read token use.</summary>
    [JsonPropertyName("cacheReadTokens")]
    public required long CacheReadTokens { get; init; }

    /// <summary>Gets prompt-cache write token use.</summary>
    [JsonPropertyName("cacheWriteTokens")]
    public required long CacheWriteTokens { get; init; }

    /// <summary>Gets total token use across all buckets.</summary>
    [JsonPropertyName("totalTokens")]
    public required long TotalTokens { get; init; }

    /// <summary>Gets the number of model calls in the sample.</summary>
    [JsonPropertyName("modelCalls")]
    public required int ModelCalls { get; init; }

    /// <summary>Gets the sampled window start time.</summary>
    [JsonPropertyName("windowStartUtc")]
    public required DateTimeOffset WindowStartUtc { get; init; }

    /// <summary>Gets the latest model activity time.</summary>
    [JsonPropertyName("lastActivityUtc")]
    public required DateTimeOffset LastActivityUtc { get; init; }
}
