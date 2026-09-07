using System;

namespace TokenHound.App.ViewModels;

/// <summary>Provides catalog lookup for known AI providers, including display names, badges, and logo resources.</summary>
internal static class ProviderCatalog
{
    private const string DEFAULT_CLAUDE_NAME = "Claude Code";
    private const string DEFAULT_CLAUDE_BADGE = "C";
    private const string DEFAULT_MOCK_NAME = "Mock Provider";
    private const string DEFAULT_MOCK_BADGE = "M";

    /// <summary>Resolves the human-readable display name for a given provider identifier.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <returns>The official name of the provider, or the provider ID if unmapped.</returns>
    public static string ResolveDefaultName(string providerId)
        => providerId.ToLowerInvariant() switch
        {
            "claude" => DEFAULT_CLAUDE_NAME,
            "gemini" or "antigravity" => "Antigravity",
            "codex" => "Codex",
            "cursor" => "Cursor",
            "copilot" => "Copilot",
            "glm" => "GLM",
            "grok" => "Grok",
            "perplexity" => "Perplexity",
            "mock" => DEFAULT_MOCK_NAME,
            _ => providerId
        };

    /// <summary>Resolves the short text glyph badge for a given provider identifier.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <returns>The short acronym or initial for the provider.</returns>
    public static string ResolveDefaultBadge(string providerId)
        => providerId.ToLowerInvariant() switch
        {
            "claude" => DEFAULT_CLAUDE_BADGE,
            "gemini" or "antigravity" => "G",
            "codex" => "X",
            "cursor" => "Cu",
            "copilot" => "Cp",
            "glm" => "GL",
            "grok" => "Gr",
            "perplexity" => "P",
            "mock" => DEFAULT_MOCK_BADGE,
            _ => providerId.Length > 0 ? providerId[..1].ToUpperInvariant() : "?"
        };

    /// <summary>Resolves the pack URI for the official provider logo resource.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <returns>The WPF pack URI to the embedded PNG asset, or null if no logo is available.</returns>
    public static string? ResolveLogoSource(string providerId)
        => providerId.ToLowerInvariant() switch
        {
            "claude" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/claude.png",
            "gemini" or "antigravity" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/antigravity.png",
            "codex" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/codex.png",
            "cursor" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/cursor.png",
            "copilot" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/copilot.png",
            "glm" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/glm.png",
            "grok" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/grok.png",
            "perplexity" => "pack://application:,,,/TokenHound.App;component/Assets/Logos/perplexity.png",
            _ => null
        };
}
