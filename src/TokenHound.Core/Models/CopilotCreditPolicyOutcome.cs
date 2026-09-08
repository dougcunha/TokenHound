namespace TokenHound.Core.Models;

/// <summary>
/// Describes the result of applying the pure Copilot credit policy.
/// </summary>
public enum CopilotCreditPolicyOutcome
{
    /// <summary>
    /// Compatible data produced a complete calculation.
    /// </summary>
    Valid,

    /// <summary>
    /// Compatible data produced a usage reading with missing dimensions or coverage.
    /// </summary>
    Partial,

    /// <summary>
    /// No item matched the verified product, unit, and optional filters.
    /// </summary>
    UnsupportedData,

    /// <summary>
    /// The input contained invalid metadata or quantities.
    /// </summary>
    InvalidData
}
