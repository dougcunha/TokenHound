using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

public static partial class ProviderUsageRowFactory
{
    private const string CLAUDE_PROVIDER_ID = "claude";
    private const string CLAUDE_PROFILE_PREFIX = "claude-";
    private const string CLAUDE_SESSION_WINDOW = "five_hour";
    private const string CLAUDE_WEEKLY_WINDOW = "seven_day";
    private const string CLAUDE_SCOPED_SUFFIX = "_scoped";
    private const string CLAUDE_WEEKLY_SCOPED_WINDOW = "weekly_scoped";
    private const string CLAUDE_SESSION_LABEL = "Current session";
    private const string CLAUDE_WEEKLY_LABEL = "Current week (all models)";
    private static readonly string[] CLAUDE_MODEL_WEEKLY_PREFIXES = ["seven_day_", "weekly_"];

    /// <summary>
    /// Determines whether a provider identifier belongs to the default or a named Claude profile.
    /// </summary>
    /// <param name="providerId">The provider identifier to test.</param>
    /// <returns><see langword="true"/> for <c>claude</c> and <c>claude-&lt;slug&gt;</c> identifiers.</returns>
    internal static bool IsClaudeProvider(string providerId)
        => string.Equals(providerId, CLAUDE_PROVIDER_ID, StringComparison.OrdinalIgnoreCase)
            || providerId.StartsWith(CLAUDE_PROFILE_PREFIX, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the canonical Claude session or overall weekly window by exact name, never an additional quota.
    /// </summary>
    /// <param name="windows">The Claude snapshot windows.</param>
    /// <param name="isSession">Whether to find the five-hour session instead of the overall weekly window.</param>
    /// <returns>The matching canonical window, or <see langword="null"/> when it was not reported.</returns>
    internal static LimitWindow? FindClaudeBaseWindow(IReadOnlyList<LimitWindow> windows, bool isSession)
    {

        var name = isSession ? CLAUDE_SESSION_WINDOW : CLAUDE_WEEKLY_WINDOW;

        return windows.FirstOrDefault(window => string.Equals(window.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveClaudeLabel(LimitWindow window)
    {

        var name = window.Name;

        if (string.Equals(name, CLAUDE_SESSION_WINDOW, StringComparison.OrdinalIgnoreCase))
            return CLAUDE_SESSION_LABEL;

        if (string.Equals(name, CLAUDE_WEEKLY_WINDOW, StringComparison.OrdinalIgnoreCase))
            return CLAUDE_WEEKLY_LABEL;

        if (name.EndsWith(CLAUDE_SCOPED_SUFFIX, StringComparison.OrdinalIgnoreCase))
            return ResolveClaudeScopedLabel(name, window.GroupName);

        var prefix = CLAUDE_MODEL_WEEKLY_PREFIXES.FirstOrDefault(
            candidate => name.StartsWith(candidate, StringComparison.OrdinalIgnoreCase) && name.Length > candidate.Length);

        return prefix is null ? Humanize(name) : $"Current week ({Humanize(name[prefix.Length..])})";
    }

    private static string ResolveClaudeScopedLabel(string name, string? scope)
    {

        var scopeText = string.IsNullOrWhiteSpace(scope) ? "scoped" : scope;

        if (string.Equals(name, CLAUDE_WEEKLY_SCOPED_WINDOW, StringComparison.OrdinalIgnoreCase))
            return $"Current week ({scopeText})";

        return $"{Humanize(name[..^CLAUDE_SCOPED_SUFFIX.Length])} ({scopeText})";
    }

    private static string Humanize(string identifier)
    {

        var words = identifier.Replace('_', ' ').Trim();

        return words.Length == 0 ? identifier : char.ToUpperInvariant(words[0]) + words[1..].ToLowerInvariant();
    }
}
