namespace TokenHound.Core.Models;

/// <summary>
/// Classifies a Copilot plan independently of its billing owner scope.
/// </summary>
public enum CopilotPlanType
{
    /// <summary>
    /// The plan was not recognized.
    /// </summary>
    Unknown,

    /// <summary>
    /// A free personal plan.
    /// </summary>
    Free,

    /// <summary>
    /// A Pro personal plan.
    /// </summary>
    Pro,

    /// <summary>
    /// A Pro+ personal plan.
    /// </summary>
    ProPlus,

    /// <summary>
    /// A Max personal plan.
    /// </summary>
    Max,

    /// <summary>
    /// An organization-managed Business plan.
    /// </summary>
    Business,

    /// <summary>
    /// An enterprise-managed plan.
    /// </summary>
    Enterprise
}
