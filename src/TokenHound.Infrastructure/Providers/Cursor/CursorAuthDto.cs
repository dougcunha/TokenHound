namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Represents the authentication and account details extracted from Cursor's state database.
/// </summary>
public sealed record CursorAuthDto
{
    /// <summary>
    /// Gets the JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the WorkOS membership/account identifier.
    /// </summary>
    public required string StripeMembershipAuthId { get; init; }

    /// <summary>
    /// Gets the cached user email address, if available.
    /// </summary>
    public string? CachedEmail { get; init; }

    /// <summary>
    /// Gets the user subscription membership tier (e.g., free, pro).
    /// </summary>
    public string? StripeMembershipType { get; init; }
}
