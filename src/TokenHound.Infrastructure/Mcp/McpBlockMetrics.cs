using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains structured block data without free-form diagnostics.</summary>
public sealed record McpBlockMetrics
{
    /// <summary>Gets whether provider requests are blocked.</summary>
    [JsonPropertyName("isBlocked")]
    public required bool IsBlocked { get; init; }

    /// <summary>Gets the UTC time when the block expires, if known.</summary>
    [JsonPropertyName("resetTimeUtc")]
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>Gets the provider's retry delay in seconds, if known.</summary>
    [JsonPropertyName("retryAfterSeconds")]
    public int? RetryAfterSeconds { get; init; }
}
