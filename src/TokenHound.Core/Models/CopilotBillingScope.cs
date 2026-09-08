namespace TokenHound.Core.Models;

/// <summary>
/// Identifies the billing owner scope for Copilot usage.
/// </summary>
public enum CopilotBillingScope
{
    /// <summary>
    /// Usage billed to the personal account.
    /// </summary>
    Personal,

    /// <summary>
    /// Usage billed to an organization.
    /// </summary>
    Organization,

    /// <summary>
    /// Usage billed to an enterprise.
    /// </summary>
    Enterprise,

    /// <summary>
    /// The billing owner has not been resolved.
    /// </summary>
    Unknown
}
