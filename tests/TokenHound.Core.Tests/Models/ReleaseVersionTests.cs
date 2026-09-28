using AwesomeAssertions;
using System;
using TokenHound.Core.Models;

namespace TokenHound.Core.Tests.Models;

/// <summary>
/// Verifies parsing and ordering of <see cref="ReleaseVersion"/> (TC-01, TC-02).
/// </summary>
public sealed class ReleaseVersionTests
{
    /// <summary>
    /// Verifies that tags, informational versions, and prerelease labels parse into their parts.
    /// </summary>
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3, null)]
    [InlineData("V10.0.1", 10, 0, 1, null)]
    [InlineData("1.2.3", 1, 2, 3, null)]
    [InlineData("1.2.3+abc123def", 1, 2, 3, null)]
    [InlineData("1.2.3-beta.1", 1, 2, 3, "beta.1")]
    [InlineData("v2.0.0-rc.2+build.7", 2, 0, 0, "rc.2")]
    [InlineData("  0.4.0  ", 0, 4, 0, null)]
    public void TryParse_WithValidText_ReturnsParts(string text, int major, int minor, int patch, string? prerelease)
    {
        var parsed = ReleaseVersion.TryParse(text, out var version);

        parsed.Should().BeTrue();
        version!.Major.Should().Be(major);
        version.Minor.Should().Be(minor);
        version.Patch.Should().Be(patch);
        version.Prerelease.Should().Be(prerelease);
    }

    /// <summary>
    /// Verifies that malformed text is rejected.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Version unavailable")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("v1.x.3")]
    [InlineData("1.2.-3")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-beta..1")]
    [InlineData("1.2.3-be_ta")]
    public void TryParse_WithMalformedText_ReturnsFalse(string? text)
    {
        var parsed = ReleaseVersion.TryParse(text, out var version);

        parsed.Should().BeFalse();
        version.Should().BeNull();
    }

    /// <summary>
    /// Verifies ordering across major, minor, patch, and prerelease labels.
    /// </summary>
    [Theory]
    [InlineData("1.2.4", "1.2.3", 1)]
    [InlineData("1.3.0", "1.2.9", 1)]
    [InlineData("2.0.0", "1.99.99", 1)]
    [InlineData("1.2.3", "1.2.3", 0)]
    [InlineData("1.2.3+meta", "1.2.3", 0)]
    [InlineData("1.2.2", "1.2.3", -1)]
    [InlineData("1.2.3-beta.1", "1.2.3", -1)]
    [InlineData("1.2.3", "1.2.3-rc.1", 1)]
    [InlineData("1.2.3-beta.2", "1.2.3-beta.10", -1)]
    [InlineData("1.2.3-beta", "1.2.3-beta.1", -1)]
    [InlineData("1.2.3-1", "1.2.3-alpha", -1)]
    [InlineData("1.2.3-alpha", "1.2.3-beta", -1)]
    public void CompareTo_OrdersVersions(string left, string right, int expectedSign)
    {
        ReleaseVersion.TryParse(left, out var leftVersion).Should().BeTrue();
        ReleaseVersion.TryParse(right, out var rightVersion).Should().BeTrue();

        Math.Sign(leftVersion!.CompareTo(rightVersion)).Should().Be(expectedSign);
        leftVersion.IsNewerThan(rightVersion!).Should().Be(expectedSign > 0);
    }

    /// <summary>
    /// Verifies that the text form drops the tag prefix and build metadata.
    /// </summary>
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3-beta.1+sha", "1.2.3-beta.1")]
    public void ToString_ReturnsNormalizedVersion(string text, string expected)
    {
        ReleaseVersion.TryParse(text, out var version).Should().BeTrue();

        version!.ToString().Should().Be(expected);
    }
}
