using AwesomeAssertions;
using System;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies update evaluation and periodic check due-ness in <see cref="UpdatePolicy"/> (TC-03, TC-04).
/// </summary>
public sealed class UpdatePolicyTests
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies the status for newer, equal, older, and prerelease tags against version 1.2.3.
    /// </summary>
    [Theory]
    [InlineData("v1.2.4", UpdateCheckStatus.Available)]
    [InlineData("v2.0.0", UpdateCheckStatus.Available)]
    [InlineData("v1.2.3", UpdateCheckStatus.UpToDate)]
    [InlineData("v1.2.2", UpdateCheckStatus.UpToDate)]
    [InlineData("v1.3.0-beta.1", UpdateCheckStatus.UpToDate)]
    [InlineData("nightly", UpdateCheckStatus.Unavailable)]
    public void Evaluate_ComparesTagWithCurrentVersion(string tag, UpdateCheckStatus expected)
    {
        var outcome = UpdatePolicy.Evaluate(Version("1.2.3"), Release(tag), null);

        outcome.Status.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that a newer release flagged as prerelease or draft is never an update.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Evaluate_WithPrereleaseOrDraftFlag_ReturnsUpToDate(bool isPrerelease, bool isDraft)
    {
        var release = Release("v9.0.0") with { IsPrerelease = isPrerelease, IsDraft = isDraft };

        var outcome = UpdatePolicy.Evaluate(Version("1.2.3"), release, null);

        outcome.Status.Should().Be(UpdateCheckStatus.UpToDate);
    }

    /// <summary>
    /// Verifies that the skipped version is suppressed and a newer tag becomes available again.
    /// </summary>
    [Theory]
    [InlineData("v1.3.0", "1.3.0", UpdateCheckStatus.Skipped)]
    [InlineData("v1.3.0", "v1.3.0", UpdateCheckStatus.Skipped)]
    [InlineData("v1.3.1", "1.3.0", UpdateCheckStatus.Available)]
    [InlineData("v1.3.0", "garbage", UpdateCheckStatus.Available)]
    public void Evaluate_WithSkippedVersion_SuppressesOnlyThatVersion(string tag, string skipped, UpdateCheckStatus expected)
    {
        var outcome = UpdatePolicy.Evaluate(Version("1.2.3"), Release(tag), skipped);

        outcome.Status.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that an available outcome carries the release and its parsed version.
    /// </summary>
    [Fact]
    public void Evaluate_WhenAvailable_CarriesReleaseAndVersion()
    {
        var release = Release("v1.4.0");

        var outcome = UpdatePolicy.Evaluate(Version("1.2.3"), release, null);

        outcome.Release.Should().BeSameAs(release);
        outcome.LatestVersion!.ToString().Should().Be("1.4.0");
    }

    /// <summary>
    /// Verifies that a missing release is reported as up to date.
    /// </summary>
    [Fact]
    public void Evaluate_WithoutRelease_ReturnsUpToDate()
    {
        var outcome = UpdatePolicy.Evaluate(Version("1.2.3"), null, null);

        outcome.Status.Should().Be(UpdateCheckStatus.UpToDate);
    }

    /// <summary>
    /// Verifies that a check is due without a recorded check and after the interval elapses.
    /// </summary>
    [Theory]
    [InlineData(null, 24, true)]
    [InlineData(23.99, 24, false)]
    [InlineData(24.0, 24, true)]
    [InlineData(30.0, 24, true)]
    [InlineData(0.5, 1, false)]
    [InlineData(-2.0, 24, true)]
    public void IsCheckDue_UsesElapsedHours(double? hoursSinceLastCheck, int intervalHours, bool expected)
    {
        DateTimeOffset? lastCheck = hoursSinceLastCheck is null ? null : NOW - TimeSpan.FromHours(hoursSinceLastCheck.Value);

        UpdatePolicy.IsCheckDue(NOW, lastCheck, intervalHours, true).Should().Be(expected);
    }

    /// <summary>
    /// Verifies that disabled checks or a zero interval are never due.
    /// </summary>
    [Theory]
    [InlineData(false, 24)]
    [InlineData(true, 0)]
    [InlineData(true, -5)]
    public void IsCheckDue_WhenDisabledOrZeroInterval_ReturnsFalse(bool enabled, int intervalHours)
    {
        UpdatePolicy.IsCheckDue(NOW, null, intervalHours, enabled).Should().BeFalse();
    }

    private static ReleaseVersion Version(string text)
    {
        ReleaseVersion.TryParse(text, out var version).Should().BeTrue();

        return version!;
    }

    private static ReleaseInfo Release(string tag)
        => new() { TagName = tag };
}
