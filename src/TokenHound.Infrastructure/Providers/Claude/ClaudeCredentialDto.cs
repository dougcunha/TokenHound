using System;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents local Claude Code OAuth credentials extracted from the filesystem.
/// </summary>
public sealed record ClaudeCredentialDto
{
    /// <summary>
    /// Gets the OAuth access token for Claude Code API communication.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the token expiration timestamp in UTC, if specified.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the credential token has expired relative to UTC now.
    /// </summary>
    public bool IsExpired
        => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Determines whether the credential token has expired relative to the specified timestamp.
    /// </summary>
    /// <param name="utcNow">The UTC timestamp to compare against.</param>
    /// <returns><see langword="true"/> if the token has expired; otherwise, <see langword="false"/>.</returns>
    public bool IsExpiredAt(DateTimeOffset utcNow)
        => ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;
}
