namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents an identified Claude Code configuration profile on disk.
/// </summary>
/// <param name="ProviderId">The unique provider identifier (e.g. "claude" or "claude-work").</param>
/// <param name="DisplayName">The human-readable display name (e.g. "Claude Code" or "Claude Code (work)").</param>
/// <param name="DirectoryPath">The absolute path to the profile configuration directory.</param>
/// <param name="Slug">The profile slug extracted from the directory name, or null for default.</param>
public sealed record ClaudeProfile(
    string ProviderId,
    string DisplayName,
    string DirectoryPath,
    string? Slug);
