using System;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents a borrowed Copilot bearer credential and its non-sensitive source classification.
/// </summary>
public sealed record CopilotCredential
{
    /// <summary>
    /// Gets the bearer token for the lifetime of the current request.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the source classification of the borrowed token.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Determines whether the token has a personal-access-token prefix that Copilot rejects.
    /// </summary>
    /// <param name="accessToken">The token to classify.</param>
    /// <returns><see langword="true"/> when the token has an allowlisted PAT prefix.</returns>
    public static bool IsPatShaped(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return accessToken.StartsWith("ghp_", StringComparison.Ordinal)
            || accessToken.StartsWith("github_pat_", StringComparison.Ordinal);
    }
}
