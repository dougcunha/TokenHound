using System;
using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// The subset of a GitHub release that the update flow needs.
/// </summary>
public sealed record ReleaseInfo
{
    /// <summary>
    /// Gets the release tag, for example <c>v1.2.3</c>.
    /// </summary>
    public required string TagName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the release is marked as a prerelease.
    /// </summary>
    public bool IsPrerelease { get; init; }

    /// <summary>
    /// Gets a value indicating whether the release is an unpublished draft.
    /// </summary>
    public bool IsDraft { get; init; }

    /// <summary>
    /// Gets the release page URL used for release notes and manual downloads, when known.
    /// </summary>
    public Uri? HtmlUrl { get; init; }

    /// <summary>
    /// Gets the files attached to the release.
    /// </summary>
    public IReadOnlyList<ReleaseAsset> Assets { get; init; } = [];
}
