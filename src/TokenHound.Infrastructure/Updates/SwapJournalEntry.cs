using System;
using System.Collections.Generic;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// The persisted record of a portable swap: which files were replaced in which folder.
/// </summary>
public sealed record SwapJournalEntry
{
    /// <summary>
    /// Gets the application folder whose files were swapped.
    /// </summary>
    public required string AppDirectory { get; init; }

    /// <summary>
    /// Gets the swapped file paths, relative to <see cref="AppDirectory"/>.
    /// </summary>
    public required IReadOnlyList<string> Files { get; init; }

    /// <summary>
    /// Gets when the swap started.
    /// </summary>
    public required DateTimeOffset CreatedUtc { get; init; }
}
