using System;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents borrowed Cline account credentials and their reported expiry.
/// </summary>
public sealed record ClineAuthDto
{
    /// <summary>
    /// Gets the borrowed Cline access token exactly as stored, including the <c>workos:</c> scheme prefix.
    /// </summary>
    /// <remarks>
    /// The Cline account API answers HTTP 401 when the scheme prefix is stripped, so the stored value
    /// is forwarded verbatim as the Bearer credential.
    /// </remarks>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the UTC expiry of the borrowed access token, when the store reports one.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets the borrowed Cline account identifier, when the store reports one.
    /// </summary>
    public string? AccountId { get; init; }

    /// <summary>
    /// Gets the non-sensitive source classification of the discovered credential.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Reports whether the borrowed access token has already expired.
    /// </summary>
    /// <param name="nowUtc">The reference current time.</param>
    /// <returns><see langword="true"/> when an expiry is known and not after <paramref name="nowUtc"/>.</returns>
    public bool HasExpired(DateTimeOffset nowUtc)
        => ExpiresAtUtc is { } expiresAt && expiresAt <= nowUtc;
}
