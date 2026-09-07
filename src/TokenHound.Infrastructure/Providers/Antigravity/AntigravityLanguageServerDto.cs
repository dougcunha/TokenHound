using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Represents a quota bucket returned by the Antigravity Language Server.
/// </summary>
public sealed record AntigravityBucketDto
{
    /// <summary>
    /// Gets the identifier of the quota bucket.
    /// </summary>
    [JsonPropertyName("bucketId")]
    public string? BucketId { get; init; }

    /// <summary>
    /// Gets the display name of the quota bucket.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the remaining quota fraction between 0.0 and 1.0.
    /// </summary>
    [JsonPropertyName("remainingFraction")]
    public double? RemainingFraction { get; init; }

    /// <summary>
    /// Gets the timestamp when this quota window resets.
    /// </summary>
    [JsonPropertyName("resetTime")]
    public DateTimeOffset? ResetTime { get; init; }

    /// <summary>
    /// Gets the quota window periodicity type, such as "5h" or "weekly".
    /// </summary>
    [JsonPropertyName("window")]
    public string? Window { get; init; }
}

/// <summary>
/// Represents a group of quota buckets for a specific model or tier.
/// </summary>
public sealed record AntigravityGroupDto
{
    /// <summary>
    /// Gets the display name of the model group.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the list of quota buckets within this group.
    /// </summary>
    [JsonPropertyName("buckets")]
    public IReadOnlyList<AntigravityBucketDto>? Buckets { get; init; }
}

/// <summary>
/// Represents the root envelope of the quota summary response.
/// </summary>
public sealed record AntigravityQuotaEnvelope
{
    /// <summary>
    /// Gets the nested response object containing groups.
    /// </summary>
    [JsonPropertyName("response")]
    public AntigravityQuotaSummaryResponse? Response { get; init; }
}

/// <summary>
/// Represents the quota summary response body containing bucket groups.
/// </summary>
public sealed record AntigravityQuotaSummaryResponse
{
    /// <summary>
    /// Gets the groups of quota buckets returned by the server.
    /// </summary>
    [JsonPropertyName("groups")]
    public IReadOnlyList<AntigravityGroupDto>? Groups { get; init; }
}
