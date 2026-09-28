using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;
using static TokenHound.Infrastructure.Tests.ViewModels.UpdateViewModelFixtures;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the check states, distinct messages, error handling, and re-entry guard of
/// <see cref="UpdateViewModel"/> (TC-18, check part).
/// </summary>
public sealed class UpdateViewModelTests
{
    /// <summary>Verifies that an up-to-date outcome renders the up-to-date message with the running version.</summary>
    [Fact]
    public async Task CheckAsync_WhenUpToDate_ShowsUpToDateMessage()
    {

        var sut = CreateSut(Outcome(UpdateCheckStatus.UpToDate));

        await sut.CheckAsync();

        sut.State.Should().Be(UpdateDialogState.UpToDate);
        sut.Message.Should().Be("TokenHound 1.2.0 is up to date.");
        sut.IsResult.Should().BeTrue();
        sut.IsAvailable.Should().BeFalse();
        sut.CanRetry.Should().BeFalse();
    }

    /// <summary>Verifies that an available outcome renders both versions, the offer, and the release-notes link.</summary>
    [Fact]
    public async Task CheckAsync_WhenAvailable_ShowsOfferWithVersionsAndReleaseNotes()
    {

        var sut = CreateSut(AvailableOutcome(UpdateCheckStatus.Available));

        await sut.CheckAsync();

        sut.State.Should().Be(UpdateDialogState.Available);
        sut.Message.Should().Be("TokenHound 1.3.0 is available.");
        sut.CurrentVersionText.Should().Be("1.2.0");
        sut.LatestVersionText.Should().Be("1.3.0");
        sut.HasReleaseNotes.Should().BeTrue();
        sut.IsAvailable.Should().BeTrue();
        sut.IsResult.Should().BeFalse();
    }

    /// <summary>Verifies that a manual check still offers a previously skipped version, with distinct copy.</summary>
    [Fact]
    public async Task CheckAsync_WhenSkipped_OffersVersionWithSkippedCopy()
    {

        var sut = CreateSut(AvailableOutcome(UpdateCheckStatus.Skipped));

        await sut.CheckAsync();

        sut.State.Should().Be(UpdateDialogState.Available);
        sut.Message.Should().Be(UpdateMessages.Available("1.3.0", skipped: true));
        sut.Message.Should().NotBe(UpdateMessages.Available("1.3.0", skipped: false));
    }

    /// <summary>Verifies that a rate-limited outcome renders the resume time in the clock's local zone.</summary>
    [Fact]
    public async Task CheckAsync_WhenRateLimited_ShowsLocalResumeTime()
    {

        var outcome = Outcome(UpdateCheckStatus.RateLimited) with { RetryAfterUtc = NOW.AddMinutes(45) };
        var sut = CreateSut(outcome);

        await sut.CheckAsync();

        sut.State.Should().Be(UpdateDialogState.RateLimited);
        sut.Message.Should().Be("GitHub limited update checks. Checks resume at 12:45.");
        sut.IsResult.Should().BeTrue();
    }

    /// <summary>Verifies that a resume time on another day includes the date.</summary>
    [Fact]
    public void RateLimitedMessage_WhenResumeIsAnotherDay_IncludesDate()
    {

        var message = UpdateMessages.RateLimited(NOW.AddHours(13), new ManualTimeProvider(NOW));

        message.Should().Be("GitHub limited update checks. Checks resume at 2026-09-29 01:00.");
        UpdateMessages.RateLimited(null, new ManualTimeProvider(NOW)).Should().Be(UpdateMessages.RATE_LIMITED);
    }

    /// <summary>Verifies that failed and unavailable outcomes render distinct error messages with the reason.</summary>
    [Fact]
    public async Task CheckAsync_WhenFailedOrUnavailable_ShowsDistinctErrorsWithReason()
    {

        var failed = CreateSut(Outcome(UpdateCheckStatus.Failed) with { Reason = "timeout" });
        var unavailable = CreateSut(Outcome(UpdateCheckStatus.Unavailable) with { Reason = "no version" });

        await failed.CheckAsync();
        await unavailable.CheckAsync();

        failed.State.Should().Be(UpdateDialogState.Error);
        failed.Message.Should().Be(UpdateMessages.CHECK_FAILED);
        failed.Detail.Should().Be("timeout");
        failed.HasDetail.Should().BeTrue();
        failed.CanRetry.Should().BeTrue();
        unavailable.State.Should().Be(UpdateDialogState.Error);
        unavailable.Message.Should().Be(UpdateMessages.UNAVAILABLE);
        unavailable.Detail.Should().Be("no version");
    }

    /// <summary>Verifies that every result state renders a distinct message.</summary>
    [Fact]
    public async Task CheckAsync_EachResultState_RendersDistinctMessage()
    {

        var outcomes = new[]
        {
            Outcome(UpdateCheckStatus.UpToDate),
            AvailableOutcome(UpdateCheckStatus.Available),
            Outcome(UpdateCheckStatus.RateLimited) with { RetryAfterUtc = NOW.AddMinutes(5) },
            Outcome(UpdateCheckStatus.Failed),
        };
        var messages = new List<string>();

        foreach (var outcome in outcomes)
        {
            var sut = CreateSut(outcome);
            await sut.CheckAsync();
            messages.Add(sut.Message);
        }

        messages.Should().OnlyHaveUniqueItems();
    }

    /// <summary>Verifies that a throwing check delegate becomes an error state instead of an exception.</summary>
    [Fact]
    public async Task CheckAsync_WhenDelegateThrows_ShowsErrorWithoutThrowing()
    {

        var sut = new UpdateViewModel(Dependencies(static _ => throw new InvalidOperationException("boom")), new ManualTimeProvider(NOW));

        var act = sut.CheckAsync;

        await act.Should().NotThrowAsync();
        sut.State.Should().Be(UpdateDialogState.Error);
        sut.Message.Should().Be(UpdateMessages.CHECK_FAILED);
        sut.Detail.Should().Be("boom");
    }

    /// <summary>Verifies that a second check request while one runs does not start another check.</summary>
    [Fact]
    public async Task CheckAsync_WhileCheckRuns_DoesNotStartAnother()
    {

        var pending = new TaskCompletionSource<UpdateCheckOutcome>();
        var calls = 0;
        var sut = new UpdateViewModel(
            Dependencies(_ =>
            {
                calls++;

                return pending.Task;
            }),
            new ManualTimeProvider(NOW)
        );

        var first = sut.CheckAsync();
        sut.IsBusy.Should().BeTrue();
        sut.IsChecking.Should().BeTrue();

        await sut.CheckAsync();
        sut.StartCheck();

        calls.Should().Be(1);

        pending.SetResult(Outcome(UpdateCheckStatus.UpToDate));
        await first;

        sut.IsBusy.Should().BeFalse();
        sut.State.Should().Be(UpdateDialogState.UpToDate);
    }
}
