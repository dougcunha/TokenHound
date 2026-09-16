using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Root envelope returned by the Cline account API, carrying the payload under <c>data</c>.
/// </summary>
/// <typeparam name="T">The payload type carried by <c>data</c>.</typeparam>
public sealed record ClineEnvelope<T>
{
    /// <summary>
    /// Gets a value indicating whether the account API reported success.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Gets the response payload, when one was returned.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>
    /// Gets the server-reported error description, when the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Represents the Cline account identity returned by <c>GET /api/v1/users/me</c>.
/// </summary>
public sealed record ClineAccountUser
{
    /// <summary>
    /// Gets the Cline account identifier used to address user-scoped endpoints.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the account display name, when reported.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the account organizations, when reported.
    /// </summary>
    [JsonPropertyName("organizations")]
    public IReadOnlyList<ClineAccountOrganization>? Organizations { get; init; }
}

/// <summary>
/// Represents a Cline organization membership.
/// </summary>
public sealed record ClineAccountOrganization
{
    /// <summary>
    /// Gets the organization display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether this organization is the active scope.
    /// </summary>
    [JsonPropertyName("active")]
    public bool Active { get; init; }
}

/// <summary>
/// Represents the credit balance returned by <c>GET /api/v1/users/{id}/balance</c>.
/// </summary>
public sealed record ClineAccountBalance
{
    /// <summary>
    /// Gets the reported credit balance, which carries no published denominator.
    /// </summary>
    [JsonPropertyName("balance")]
    public double? Balance { get; init; }
}

/// <summary>
/// Represents the active plan returned by <c>GET /api/v1/users/me/plan</c>.
/// </summary>
public sealed record ClineCurrentPlan
{
    /// <summary>
    /// Gets the plan catalog entry currently attached to the account.
    /// </summary>
    [JsonPropertyName("plan")]
    public ClinePlan? Plan { get; init; }

    /// <summary>
    /// Gets the plan entitlements, when the endpoint reports them at the root of the payload.
    /// </summary>
    [JsonPropertyName("entitlements")]
    public ClineEntitlements? Entitlements { get; init; }
}

/// <summary>
/// Represents a Cline subscription plan catalog entry.
/// </summary>
public sealed record ClinePlan
{
    /// <summary>
    /// Gets the plan display name shown to the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the plan interval (for example <c>Monthly</c>).
    /// </summary>
    [JsonPropertyName("interval")]
    public string? Interval { get; init; }

    /// <summary>
    /// Gets the plan entitlements carrying the inference caps.
    /// </summary>
    [JsonPropertyName("entitlements")]
    public ClineEntitlements? Entitlements { get; init; }
}

/// <summary>
/// Represents the entitlement set attached to a Cline plan.
/// </summary>
public sealed record ClineEntitlements
{
    /// <summary>
    /// Gets the Cline Pass entitlement, when the plan includes it.
    /// </summary>
    [JsonPropertyName("cline_pass")]
    public ClinePassEntitlement? ClinePass { get; init; }
}

/// <summary>
/// Represents the Cline Pass entitlement and its inference cost caps.
/// </summary>
public sealed record ClinePassEntitlement
{
    /// <summary>
    /// Gets a value indicating whether Cline Pass inference is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the rolling inference cost caps enforced per user.
    /// </summary>
    [JsonPropertyName("inferenceCapThreshold")]
    public ClineInferenceCapThreshold? InferenceCapThreshold { get; init; }
}

/// <summary>
/// Represents the rolling inference cost caps published by Cline Pass.
/// </summary>
/// <remarks>
/// The caps share the unit of the <c>costUsd</c> field reported by the usage endpoint, so their ratio
/// is a valid utilization fraction without any unit conversion.
/// </remarks>
public sealed record ClineInferenceCapThreshold
{
    /// <summary>
    /// Gets the rolling five hour cost cap per user.
    /// </summary>
    [JsonPropertyName("last5HoursUsageCostUSDPerUser")]
    public double? Last5HoursCost { get; init; }

    /// <summary>
    /// Gets the rolling seven day cost cap per user.
    /// </summary>
    [JsonPropertyName("last7daysUsageCostUSDPerUser")]
    public double? Last7DaysCost { get; init; }

    /// <summary>
    /// Gets the rolling thirty day cost cap per user.
    /// </summary>
    [JsonPropertyName("last30daysUsageCostUSDPerUser")]
    public double? Last30DaysCost { get; init; }
}