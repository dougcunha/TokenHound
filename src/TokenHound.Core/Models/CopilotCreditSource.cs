namespace TokenHound.Core.Models;

/// <summary>
/// Identifies the source family that produced Copilot credit usage.
/// </summary>
public enum CopilotCreditSource
{
    /// <summary>
    /// A billing usage API response.
    /// </summary>
    BillingApi,

    /// <summary>
    /// A daily per-user Copilot usage report.
    /// </summary>
    DailyUserReport
}
