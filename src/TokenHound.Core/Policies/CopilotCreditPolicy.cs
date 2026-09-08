using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

/// <summary>
/// Aggregates compatible Copilot billing dimensions and guards derived values.
/// </summary>
public static class CopilotCreditPolicy
{
    /// <summary>
    /// Evaluates one verified source response and applies its allowance, when compatible.
    /// </summary>
    /// <param name="request">The source metadata, verified filter, and parsed dimensions.</param>
    /// <returns>The usage dimensions and typed policy outcome.</returns>
    public static Result Evaluate(CopilotCreditAggregationRequest request)
    {

        ArgumentNullException.ThrowIfNull(request);

        return CopilotCreditPolicyEvaluator.Evaluate(request);
    }

    /// <summary>
    /// Contains the usage result and policy qualification for one source response.
    /// </summary>
    public sealed record Result
    {
        /// <summary>
        /// Gets the resulting usage, including only compatible derived values.
        /// </summary>
        public required CopilotCreditUsage Usage { get; init; }

        /// <summary>
        /// Gets the typed policy outcome.
        /// </summary>
        public required CopilotCreditPolicyOutcome Outcome { get; init; }

        /// <summary>
        /// Gets concise machine-readable policy issue identifiers.
        /// </summary>
        public IReadOnlyList<string> Issues { get; init; } = [];
    }
}
