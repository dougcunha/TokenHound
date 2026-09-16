using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents the usage transaction page returned by <c>GET /api/v1/users/{id}/usages</c>.
/// </summary>
public sealed record ClineUsageData
{
    /// <summary>
    /// Gets the newest usage transactions, ordered newest first by the account API.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<ClineUsageTransaction>? Items { get; init; }
}

/// <summary>
/// Represents a single Cline inference usage transaction.
/// </summary>
public sealed record ClineUsageTransaction
{
    /// <summary>
    /// Gets the UTC timestamp when the inference completed.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets the inference cost in the Cline cost unit shared with the published plan caps.
    /// </summary>
    [JsonPropertyName("costUsd")]
    public double? CostUsd { get; init; }

    /// <summary>
    /// Gets the credits consumed by the transaction, which is zero for free model tiers.
    /// </summary>
    [JsonPropertyName("creditsUsed")]
    public double? CreditsUsed { get; init; }

    /// <summary>
    /// Gets the number of prompt tokens reported for the transaction.
    /// </summary>
    [JsonPropertyName("promptTokens")]
    public long? PromptTokens { get; init; }

    /// <summary>
    /// Gets the number of completion tokens reported for the transaction.
    /// </summary>
    [JsonPropertyName("completionTokens")]
    public long? CompletionTokens { get; init; }

    /// <summary>
    /// Gets the total number of tokens reported for the transaction.
    /// </summary>
    [JsonPropertyName("totalTokens")]
    public long? TotalTokens { get; init; }

    /// <summary>
    /// Gets the number of prompt-cache read tokens reported for the transaction.
    /// </summary>
    [JsonPropertyName("cachedTokens")]
    public long? CachedTokens { get; init; }

    /// <summary>
    /// Gets the inference provider that served the transaction.
    /// </summary>
    [JsonPropertyName("aiInferenceProviderName")]
    public string? InferenceProviderName { get; init; }

    /// <summary>
    /// Gets the model name reported for the transaction.
    /// </summary>
    [JsonPropertyName("aiModelName")]
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the model tier reported for the transaction (for example <c>cline-free</c>).
    /// </summary>
    [JsonPropertyName("aiModelTypeName")]
    public string? ModelTypeName { get; init; }
}