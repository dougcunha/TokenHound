namespace TokenHound.Core.Models;

/// <summary>
/// Identifies why a Copilot billing reading is unavailable or stale.
/// </summary>
public enum CopilotBillingReason
{
    /// <summary>
    /// No error was reported.
    /// </summary>
    None,

    /// <summary>
    /// No usable Copilot credential was available.
    /// </summary>
    MissingCredential,

    /// <summary>
    /// The billing owner could not be resolved.
    /// </summary>
    UnknownScope,

    /// <summary>
    /// More than one billing owner remained possible.
    /// </summary>
    AmbiguousScope,

    /// <summary>
    /// The selected billing source denied access.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// A required historical report was unavailable.
    /// </summary>
    ReportUnavailable,

    /// <summary>
    /// A persisted request deadline prevented another request.
    /// </summary>
    RateLimited,

    /// <summary>
    /// The billing source could not be reached.
    /// </summary>
    NetworkFailure,

    /// <summary>
    /// The source returned data that could not be used safely.
    /// </summary>
    InvalidData,

    /// <summary>
    /// The billing reading could not be persisted.
    /// </summary>
    PersistenceFailure
}
