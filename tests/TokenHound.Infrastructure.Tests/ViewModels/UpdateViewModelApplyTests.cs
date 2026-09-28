using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Updates;
using Xunit;
using static TokenHound.Infrastructure.Tests.ViewModels.UpdateViewModelFixtures;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the Update now transitions of <see cref="UpdateViewModel"/>: download progress, Cancel, hand-off with
/// shutdown, specific errors, and re-entry (TC-18, apply part).
/// </summary>
public sealed class UpdateViewModelApplyTests
{
    /// <summary>Verifies download progress, then Applying and one shutdown request when the apply succeeds.</summary>
    [Fact]
    public async Task UpdateNowAsync_WhenApplied_ShowsProgressThenApplyingAndRequestsShutdown()
    {

        var shutdowns = 0;
        var pending = new TaskCompletionSource<UpdateApplyResult>();
        IProgress<double>? progress = null;
        var sut = await CreateOfferedAsync(
            (_, reporter, _) =>
            {
                progress = reporter;

                return pending.Task;
            },
            () => shutdowns++
        );

        var run = sut.UpdateNowAsync();

        sut.State.Should().Be(UpdateDialogState.Downloading);
        sut.IsDownloading.Should().BeTrue();
        progress!.Report(0.42);
        sut.ProgressPercent.Should().BeApproximately(42, 0.001);

        pending.SetResult(new UpdateApplyResult { Status = UpdateApplyStatus.Applied });
        await run;

        sut.State.Should().Be(UpdateDialogState.Applying);
        sut.Message.Should().Be(UpdateMessages.APPLYING);
        shutdowns.Should().Be(1);
    }

    /// <summary>Verifies that each failure status renders its own message, keeps the app running, and offers the release link.</summary>
    [Theory]
    [InlineData(UpdateApplyStatus.NotWritable, UpdateMessages.NOT_WRITABLE)]
    [InlineData(UpdateApplyStatus.AssetMissing, UpdateMessages.ASSET_MISSING)]
    [InlineData(UpdateApplyStatus.IntegrityFailed, UpdateMessages.INTEGRITY_FAILED)]
    [InlineData(UpdateApplyStatus.DownloadFailed, UpdateMessages.DOWNLOAD_FAILED)]
    [InlineData(UpdateApplyStatus.ApplyFailed, UpdateMessages.APPLY_FAILED)]
    public async Task UpdateNowAsync_WhenFailed_ShowsSpecificErrorWithoutShutdown(UpdateApplyStatus status, string expected)
    {

        var shutdowns = 0;
        var sut = await CreateOfferedAsync(
            (_, _, _) => Task.FromResult(new UpdateApplyResult { Status = status, Detail = "why" }),
            () => shutdowns++
        );

        await sut.UpdateNowAsync();

        sut.State.Should().Be(UpdateDialogState.Error);
        sut.Message.Should().Be(expected);
        sut.ReleaseLinkVisible.Should().BeTrue();
        sut.CanRetry.Should().BeTrue();
        shutdowns.Should().Be(0);
    }

    /// <summary>Verifies that Cancel during the download returns to the offer with the partial-download state reset.</summary>
    [Fact]
    public async Task CancelDownload_ReturnsToOfferWithoutShutdown()
    {

        var shutdowns = 0;
        var sut = await CreateOfferedAsync(
            async (_, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);

                return new UpdateApplyResult { Status = UpdateApplyStatus.Applied };
            },
            () => shutdowns++
        );

        var run = sut.UpdateNowAsync();
        sut.CancelDownload();
        await run;

        sut.State.Should().Be(UpdateDialogState.Available);
        sut.ProgressPercent.Should().Be(0);
        sut.IsBusy.Should().BeFalse();
        shutdowns.Should().Be(0);
    }

    /// <summary>Verifies that a second Update now while downloading does not start another download.</summary>
    [Fact]
    public async Task UpdateNowAsync_WhileDownloading_DoesNotStartAnother()
    {

        var calls = 0;
        var pending = new TaskCompletionSource<UpdateApplyResult>();
        var sut = await CreateOfferedAsync(
            (_, _, _) =>
            {
                calls++;

                return pending.Task;
            },
            null
        );

        var first = sut.UpdateNowAsync();
        await sut.UpdateNowAsync();
        sut.StartCheck();

        calls.Should().Be(1);

        pending.SetResult(new UpdateApplyResult { Status = UpdateApplyStatus.DownloadFailed });
        await first;
    }

    /// <summary>Verifies that Update now does nothing before an update is offered or without an apply step.</summary>
    [Fact]
    public async Task UpdateNowAsync_WithoutOfferOrApplyStep_DoesNothing()
    {

        var calls = 0;
        var dependencies = Dependencies(static _ => Task.FromResult(Outcome(UpdateCheckStatus.UpToDate))) with
        {
            ApplyAsync = (_, _, _) =>
            {
                calls++;

                return Task.FromResult(new UpdateApplyResult { Status = UpdateApplyStatus.Applied });
            },
        };
        var upToDate = new UpdateViewModel(dependencies, new ManualTimeProvider(NOW));
        var withoutApply = new UpdateViewModel(
            Dependencies(static _ => Task.FromResult(AvailableOutcome(UpdateCheckStatus.Available))),
            new ManualTimeProvider(NOW)
        );

        await upToDate.CheckAsync();
        await upToDate.UpdateNowAsync();
        await withoutApply.CheckAsync();
        await withoutApply.UpdateNowAsync();

        calls.Should().Be(0);
        withoutApply.State.Should().Be(UpdateDialogState.Available);
    }

    /// <summary>Verifies that an unexpected exception from the apply step becomes an error and keeps the app running.</summary>
    [Fact]
    public async Task UpdateNowAsync_WhenApplyThrows_ShowsErrorWithoutShutdown()
    {

        var shutdowns = 0;
        var sut = await CreateOfferedAsync(
            static (_, _, _) => throw new InvalidOperationException("boom"),
            () => shutdowns++
        );

        await sut.UpdateNowAsync();

        sut.State.Should().Be(UpdateDialogState.Error);
        sut.Message.Should().Be(UpdateMessages.APPLY_FAILED);
        sut.Detail.Should().Be("boom");
        shutdowns.Should().Be(0);
    }

    private static async Task<UpdateViewModel> CreateOfferedAsync(
        Func<ReleaseInfo, IProgress<double>, CancellationToken, Task<UpdateApplyResult>> apply,
        Action? requestShutdown)
    {

        var dependencies = Dependencies(static _ => Task.FromResult(AvailableOutcome(UpdateCheckStatus.Available))) with
        {
            ApplyAsync = apply,
            RequestShutdown = requestShutdown,
        };
        var sut = new UpdateViewModel(dependencies, new ManualTimeProvider(NOW));

        await sut.CheckAsync();

        return sut;
    }
}
