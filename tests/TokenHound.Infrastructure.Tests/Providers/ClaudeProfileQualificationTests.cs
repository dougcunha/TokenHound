using AwesomeAssertions;
using System;
using System.IO;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Verifies active Claude profile qualification and enumeration.</summary>
public sealed class ClaudeProfileQualificationTests
{
    private const string VALID_CREDENTIAL = """
        {"claudeAiOauth":{"accessToken":"sk-ant-oat01-test-token","expiresAt":1757121000000}}
        """;

    /// <summary>Verifies default and isolated profiles retain their IDs and names.</summary>
    [Fact]
    public void DiscoverProfiles_WithDefaultAndWork_ReturnsBothProfiles()
    {

        using var scope = new ProfileScope();
        var defaultDir = scope.CreateProfile(".claude", VALID_CREDENTIAL);
        var workDir = scope.CreateProfile(".claude-work", VALID_CREDENTIAL);

        var profiles = new ClaudeProfileDiscovery(scope.Root).DiscoverProfiles();

        profiles.Should().HaveCount(2);
        profiles[0].ProviderId.Should().Be("claude");
        profiles[0].DisplayName.Should().Be("Claude Code");
        profiles[0].Slug.Should().BeNull();
        profiles[0].DirectoryPath.Should().Be(defaultDir);
        profiles[1].ProviderId.Should().Be("claude-work");
        profiles[1].DisplayName.Should().Be("Claude Code (work)");
        profiles[1].Slug.Should().Be("work");
        profiles[1].DirectoryPath.Should().Be(workDir);
    }

    /// <summary>Verifies credential-free directories are included only in unfiltered enumeration.</summary>
    [Fact]
    public void DiscoverProfiles_WhenDirectoryLacksCredentials_ExcludesInactive()
    {

        using var scope = new ProfileScope();
        scope.CreateProfile(".claude", VALID_CREDENTIAL);
        scope.CreateProfile(".claude-empty");
        var discovery = new ClaudeProfileDiscovery(scope.Root);

        var activeProfiles = discovery.DiscoverProfiles(onlyActive: true);
        var allProfiles = discovery.DiscoverProfiles(onlyActive: false);

        activeProfiles.Should().ContainSingle();
        activeProfiles[0].ProviderId.Should().Be("claude");
        allProfiles.Should().HaveCount(2);
        allProfiles[1].ProviderId.Should().Be("claude-empty");
    }

    /// <summary>Verifies file presence alone does not qualify a profile as active.</summary>
    /// <param name="directoryName">The profile directory under test.</param>
    /// <param name="contents">The invalid credentials content.</param>
    [Theory]
    [InlineData(".claude", "")]
    [InlineData(".claude", "{}")]
    [InlineData(".claude", "{invalid")]
    [InlineData(".claude-work", "")]
    [InlineData(".claude-work", "{}")]
    [InlineData(".claude-work", "{invalid")]
    public void DiscoverProfiles_WithInvalidCredentials_ExcludesProfile(string directoryName, string contents)
    {

        using var scope = new ProfileScope();
        scope.CreateProfile(directoryName, contents);
        var discovery = new ClaudeProfileDiscovery(scope.Root);
        var expectedProviderId = directoryName[1..];

        discovery.DiscoverProfiles(onlyActive: true).Should().BeEmpty();
        discovery.DiscoverProfiles(onlyActive: false).Should().ContainSingle(
            profile => profile.ProviderId == expectedProviderId
        );
    }

    /// <summary>Verifies a credential file locked against reads is excluded.</summary>
    [Fact]
    public void DiscoverProfiles_WithUnreadableCredentials_ExcludesProfile()
    {

        using var scope = new ProfileScope();
        var profileDir = scope.CreateProfile(".claude-work", VALID_CREDENTIAL);
        var credentialPath = Path.Combine(profileDir, ".credentials.json");

        using (var lockedFile = new FileStream(credentialPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {

            var profiles = new ClaudeProfileDiscovery(scope.Root).DiscoverProfiles();

            profiles.Should().BeEmpty();
        }
    }

    private sealed class ProfileScope : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"claude_profiles_{Guid.NewGuid():N}");

        public string CreateProfile(string directoryName, string? credentials = null)
        {

            var directory = Path.Combine(Root, directoryName);
            Directory.CreateDirectory(directory);

            if (credentials is not null)
                File.WriteAllText(Path.Combine(directory, ".credentials.json"), credentials);

            return directory;
        }

        public void Dispose()
        {

            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
