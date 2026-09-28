using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Shared clock, outcomes, and delegate fixtures for the <see cref="UpdateViewModel"/> tests.
/// </summary>
internal static class UpdateViewModelFixtures
{
    internal static readonly DateTimeOffset NOW = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    internal static readonly Uri RELEASE_PAGE = new("https://github.com/dougcunha/TokenHound/releases/tag/v1.3.0");

    internal static UpdateViewModel CreateSut(UpdateCheckOutcome outcome, List<string>? saved = null)
        => new(Dependencies(_ => Task.FromResult(outcome), saved), new ManualTimeProvider(NOW));

    internal static UpdateDialogDependencies Dependencies(
        Func<CancellationToken, Task<UpdateCheckOutcome>> check,
        List<string>? saved = null)
        => new()
        {
            CheckAsync = check,
            SaveSkippedVersionAsync = (version, _) =>
            {
                saved?.Add(version);

                return Task.FromResult(true);
            },
            CurrentVersion = Version("1.2.0"),
        };

    internal static UpdateCheckOutcome Outcome(UpdateCheckStatus status)
        => new() { Status = status };

    internal static UpdateCheckOutcome AvailableOutcome(UpdateCheckStatus status)
        => new()
        {
            Status = status,
            Release = new ReleaseInfo { TagName = "v1.3.0", HtmlUrl = RELEASE_PAGE },
            LatestVersion = Version("1.3.0"),
        };

    internal static ReleaseVersion Version(string text)
        => ReleaseVersion.TryParse(text, out var version)
            ? version
            : throw new ArgumentException("Invalid test version.", nameof(text));
}
