namespace TokenHound.App.ViewModels;

/// <summary>Provides catalog lookup for known AI providers, including display names, badges, and glyph marks.</summary>
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
            "opencode" => "OpenCode",
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
            "opencode" => "OC",
            "glm" => "GL",
            "grok" => "Gr",
            "perplexity" => "P",
            "mock" => DEFAULT_MOCK_BADGE,
            _ => providerId.Length > 0 ? providerId[..1].ToUpperInvariant() : "?"
        };

    /// <summary>Resolves the resource key of the monochrome vector mark for a given provider identifier.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <returns>The key into <c>Assets/Logos/ProviderGlyphs.xaml</c>, or null if no mark is available.</returns>
    public static string? ResolveGlyphKey(string providerId)
        => providerId.ToLowerInvariant() switch
        {
            "claude" => "Glyph.Claude",
            "gemini" or "antigravity" => "Glyph.Antigravity",
            "codex" => "Glyph.Codex",
            "cursor" => "Glyph.Cursor",
            "copilot" => "Glyph.Copilot",
            "glm" => "Glyph.Glm",
            "grok" => "Glyph.Grok",
            "opencode" => "Glyph.Opencode",
            "perplexity" => "Glyph.Perplexity",
            _ => null
        };

    /// <summary>Resolves the optical size multiplier that evens out the ink extent of a provider mark.</summary>
    /// <remarks>
    /// Marks normalized into the same box are not marks of equal size: a thin spark reads smaller than a
    /// solid knot. Each factor brings the glyph's ink to the same extent as the Claude mark.
    /// </remarks>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <returns>The multiplier applied to the base glyph size.</returns>
    public static double ResolveGlyphScale(string providerId)
        => providerId.ToLowerInvariant() switch
        {
            "claude" => 0.9676,
            "gemini" or "antigravity" => 1.0,
            "codex" => 0.9748,
            "cursor" => 0.9699,
            "copilot" => 1.0,
            "glm" => 0.95,
            "grok" => 1.0,
            "opencode" => 1.0,
            "perplexity" => 1.0344,
            _ => 1.0
        };
}
