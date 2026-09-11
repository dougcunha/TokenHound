using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Root model representing the usage telemetry response returned by OpenCode Go's API.
/// </summary>
public sealed record OpenCodeUsageResponse
{
    /// <summary>
    /// Gets the usage windows data containing rolling, weekly, and monthly limit metrics.
    /// </summary>
    [JsonPropertyName("usage")]
    public required OpenCodeUsageData Usage { get; init; }
}

/// <summary>
/// Contains the three distinct quota limit windows tracked by OpenCode Go.
/// </summary>
public sealed record OpenCodeUsageData
{
    /// <summary>
    /// Gets the 5-hour rolling limit window.
    /// </summary>
    [JsonPropertyName("rolling")]
    public required OpenCodeLimitWindowDto Rolling { get; init; }

    /// <summary>
    /// Gets the weekly limit window.
    /// </summary>
    [JsonPropertyName("weekly")]
    public required OpenCodeLimitWindowDto Weekly { get; init; }

    /// <summary>
    /// Gets the monthly limit window.
    /// </summary>
    [JsonPropertyName("monthly")]
    public required OpenCodeLimitWindowDto Monthly { get; init; }
}

/// <summary>
/// Telemetry metrics for an individual OpenCode Go limit window.
/// </summary>
public sealed record OpenCodeLimitWindowDto
{
    /// <summary>
    /// Gets the status of the window (e.g. "ok", "rate-limited").
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets the consumed percentage of the quota window (0.0 to 100.0).
    /// </summary>
    [JsonPropertyName("percent")]
    public required double Percent { get; init; }

    /// <summary>
    /// Gets the authoritative reset timestamp for the window.
    /// </summary>
    [JsonPropertyName("resetsAt")]
    public required DateTimeOffset ResetsAt { get; init; }
}

/// <summary>
/// Error payload returned by the OpenCode Go API on 4xx/5xx responses.
/// </summary>
public sealed record OpenCodeErrorResponse
{
    /// <summary>
    /// Gets the server-reported quota reset timestamp, if supplied.
    /// </summary>
    [JsonPropertyName("resetsAt")]
    public DateTimeOffset? ResetsAt { get; init; }

    /// <summary>
    /// Gets the top-level error response classification.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// Gets the detailed error information.
    /// </summary>
    [JsonPropertyName("error")]
    public OpenCodeErrorInfo? Error { get; init; }

    /// <summary>
    /// Gets contextual metadata associated with the error.
    /// </summary>
    [JsonPropertyName("metadata")]
    public OpenCodeErrorMetadata? Metadata { get; init; }
}

/// <summary>
/// Detailed error information returned by OpenCode Go.
/// </summary>
public sealed record OpenCodeErrorInfo
{
    /// <summary>
    /// Gets the error classification type (e.g. "GoUsageLimitError", "AuthError", "EntitlementError").
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Metadata accompanying an OpenCode Go error.
/// </summary>
public sealed record OpenCodeErrorMetadata
{
    /// <summary>
    /// Gets the affected workspace identifier.
    /// </summary>
    [JsonPropertyName("workspace")]
    public string? Workspace { get; init; }

    /// <summary>
    /// Gets the name of the exhausted limit (e.g. "5 hour").
    /// </summary>
    [JsonPropertyName("limitName")]
    public string? LimitName { get; init; }
}
