using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Data transfer object representing a serialized Composer session header from Cursor's state database.
/// </summary>
public sealed record CursorComposerHeaderDto
{
    /// <summary>
    /// Gets the unique composer conversation identifier.
    /// </summary>
    [JsonPropertyName("composerId")]
    public string? ComposerId { get; init; }

    /// <summary>
    /// Gets the display name of the composer conversation.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the subtitle of the composer conversation.
    /// </summary>
    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; init; }

    /// <summary>
    /// Gets the Unix millisecond epoch timestamp when an unfinished run started, if any.
    /// </summary>
    [JsonPropertyName("unfinishedRunAt")]
    public long? UnfinishedRunAt { get; init; }

    /// <summary>
    /// Gets whether there are blocking actions pending user approval.
    /// </summary>
    [JsonPropertyName("hasBlockingPendingActions")]
    public bool? HasBlockingPendingActions { get; init; }

    /// <summary>
    /// Gets whether there is a plan pending user approval.
    /// </summary>
    [JsonPropertyName("hasPendingPlan")]
    public bool? HasPendingPlan { get; init; }

    /// <summary>
    /// Gets the Unix millisecond epoch of the last conversation checkpoint update.
    /// </summary>
    [JsonPropertyName("conversationCheckpointLastUpdatedAt")]
    public long? ConversationCheckpointLastUpdatedAt { get; init; }

    /// <summary>
    /// Gets the Unix millisecond epoch of the last update.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public long? LastUpdatedAt { get; init; }

    /// <summary>
    /// Gets the Unix millisecond epoch of creation.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether this composer session is waiting for user approval.
    /// </summary>
    public bool IsWaitingApproval
        => (HasBlockingPendingActions ?? false) || (HasPendingPlan ?? false);

    /// <summary>
    /// Gets a value indicating whether this composer session has an active unfinished run.
    /// </summary>
    public bool HasUnfinishedRun
        => UnfinishedRunAt.HasValue && UnfinishedRunAt.Value > 0;

    /// <summary>
    /// Gets the latest checkpoint or update timestamp in UTC, if available.
    /// </summary>
    public DateTimeOffset? LatestCheckpointUtc
    {
        get
        {

            var ms = ConversationCheckpointLastUpdatedAt ?? LastUpdatedAt;

            return ms.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(ms.Value)
                : null;
        }
    }
}
