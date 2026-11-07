using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.HudFeed;

/// <summary>Contains the HUD content that TokenHound publishes for other local apps, such as a Herdr mod.</summary>
public sealed record HudFeedDocument
{
    /// <summary>The schema version this build writes; additive changes keep it, breaking changes raise it.</summary>
    public const int CURRENT_SCHEMA_VERSION = 1;

    /// <summary>Gets the schema version of the document.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CURRENT_SCHEMA_VERSION;

    /// <summary>Gets the UTC time when TokenHound wrote the document.</summary>
    [JsonPropertyName("writtenAtUtc")]
    public required DateTimeOffset WrittenAtUtc { get; init; }

    /// <summary>Gets a value indicating whether TokenHound was running when it wrote the document.</summary>
    [JsonPropertyName("isRunning")]
    public required bool IsRunning { get; init; }

    /// <summary>Gets the TokenHound version that wrote the document.</summary>
    [JsonPropertyName("appVersion")]
    public required string AppVersion { get; init; }

    /// <summary>Gets the longest interval between two writes while TokenHound runs.</summary>
    [JsonPropertyName("heartbeatSeconds")]
    public required int HeartbeatSeconds { get; init; }

    /// <summary>Gets the providers in HUD order.</summary>
    [JsonPropertyName("providers")]
    public required IReadOnlyList<HudFeedProvider> Providers { get; init; }
}
