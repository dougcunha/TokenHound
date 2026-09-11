namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Represents borrowed OpenCode authentication credentials and their source classification.
/// </summary>
public sealed record OpenCodeAuthDto
{
    /// <summary>
    /// Gets the OpenCode API key used for Bearer authentication.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Gets the non-sensitive source classification of the discovered credential.
    /// </summary>
    public required string Source { get; init; }
}
