using AwesomeAssertions;
using TokenHound.App.ViewModels;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies that <see cref="ProviderCatalog"/> resolves names, badges, glyphs, and scales for standard and multi-profile providers.
/// </summary>
public sealed class ProviderCatalogTests
{
    /// <summary>
    /// Verifies that ResolveDefaultName correctly formats multi-profile Claude slugs.
    /// </summary>
    /// <param name="providerId">The provider identifier under test.</param>
    /// <param name="expectedName">The expected display name.</param>
    [Theory]
    [InlineData("claude", "Claude Code")]
    [InlineData("claude-work", "Claude Code (work)")]
    [InlineData("claude-personal", "Claude Code (personal)")]
    [InlineData("claude-", "Claude Code")]
    [InlineData("copilot", "Copilot")]
    [InlineData("cursor", "Cursor")]
    public void ResolveDefaultName_WithVariousProviders_ReturnsExpectedName(string providerId, string expectedName)
    {

        var name = ProviderCatalog.ResolveDefaultName(providerId);

        name.Should().Be(expectedName);
    }

    /// <summary>
    /// Verifies that ResolveDefaultBadge returns "C" for all Claude multi-profile variants.
    /// </summary>
    /// <param name="providerId">The Claude provider identifier under test.</param>
    [Theory]
    [InlineData("claude")]
    [InlineData("claude-work")]
    [InlineData("claude-client2")]
    public void ResolveDefaultBadge_WithClaudeProfiles_ReturnsC(string providerId)
    {

        var badge = ProviderCatalog.ResolveDefaultBadge(providerId);

        badge.Should().Be("C");
    }

    /// <summary>
    /// Verifies that ResolveGlyphKey returns "Glyph.Claude" for all Claude multi-profile variants.
    /// </summary>
    /// <param name="providerId">The Claude provider identifier under test.</param>
    [Theory]
    [InlineData("claude")]
    [InlineData("claude-work")]
    [InlineData("claude-test")]
    public void ResolveGlyphKey_WithClaudeProfiles_ReturnsGlyphClaude(string providerId)
    {

        var glyph = ProviderCatalog.ResolveGlyphKey(providerId);

        glyph.Should().Be("Glyph.Claude");
    }

    /// <summary>
    /// Verifies that ResolveGlyphScale returns the Claude optical scale (0.9676) for all Claude multi-profile variants.
    /// </summary>
    /// <param name="providerId">The Claude provider identifier under test.</param>
    [Theory]
    [InlineData("claude")]
    [InlineData("claude-work")]
    public void ResolveGlyphScale_WithClaudeProfiles_ReturnsClaudeScale(string providerId)
    {

        var scale = ProviderCatalog.ResolveGlyphScale(providerId);

        scale.Should().Be(0.9676);
    }
}
