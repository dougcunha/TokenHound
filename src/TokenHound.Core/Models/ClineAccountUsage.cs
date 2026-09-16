namespace TokenHound.Core.Models;

/// <summary>
/// Represents borrowed Cline account telemetry: the reported credit balance and the subscription scope.
/// </summary>
public sealed record ClineAccountUsage
{
    /// <summary>
    /// Gets the remaining Cline credit balance reported by the account API, if available.
    /// </summary>
    /// <remarks>
    /// The account API reports a bare credit amount without a denominator, so this value never
    /// becomes a utilization fraction.
    /// </remarks>
    public double? BalanceCredits { get; init; }

    /// <summary>
    /// Gets the display name of the active subscription plan, if the account reports one.
    /// </summary>
    public string? PlanName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the account holds a Cline Pass subscription with inference caps.
    /// </summary>
    public bool HasPassSubscription { get; init; }
}
