namespace TokenHound.Core.Models;

/// <summary>
/// Indicates the origin and confidence level of reported quota or limit metrics.
/// </summary>
public enum Fidelity
{
    /// <summary>
    /// Data is retrieved directly from an official provider API, database, or token file.
    /// </summary>
    Official,

    /// <summary>
    /// Data is inferred or derived from local logs, events, or session artifacts.
    /// </summary>
    Derived,

    /// <summary>
    /// Data is supplied manually by configuration or user input.
    /// </summary>
    Manual
}
