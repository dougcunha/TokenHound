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
/// Verifies Later, Skip this version, Update now, the release-notes link, and disposal of
/// <see cref="UpdateViewModel"/> (TC-18, check part).
/// </summary>
public sealed class UpdateViewModelActionTests
{
    /// <summary>Verifies that Later requests closing without writing the skipped version.</summary>
    [Fact]
    public async Task Later_RequestsCloseWithoutSaving()
    {

        var saved = new List<string>();
        var sut = CreateSut(AvailableOutcome(UpdateCheckStatus.Available), saved);
        var closes = 0;
        sut.CloseRequested += (_, _) => closes++;

        await sut.CheckAsync();
        sut.Later();

        closes.Should().Be(1);
        saved.Should().BeEmpty();
    }

    /// <summary>Verifies that Skip this version persists the offered version and requests closing.</summary>
    [Fact]
    public async Task SkipAsync_PersistsOfferedVersionAndRequestsClose()
    {

        var saved = new List<string>();
        var sut = CreateSut(AvailableOutcome(UpdateCheckStatus.Available), saved);
        var closes = 0;
        sut.CloseRequested += (_, _) => closes++;

        await sut.CheckAsync();
        await sut.SkipAsync();

        saved.Should().Equal("1.3.0");
        closes.Should().Be(1);
    }

    /// <summary>Verifies that a failed save keeps the dialog open with an error.</summary>
    [Fact]
    public async Task SkipAsync_WhenSaveFails_ShowsErrorAndStaysOpen()
    {

        var dependencies = Dependencies(static _ => Task.FromResult(AvailableOutcome(UpdateCheckStatus.Available))) with
        {
            SaveSkippedVersionAsync = static (_, _) => Task.FromResult(false),
        };
        var sut = new UpdateViewModel(dependencies, new ManualTimeProvider(NOW));
        var closes = 0;
        sut.CloseRequested += (_, _) => closes++;

        await sut.CheckAsync();
        await sut.SkipAsync();

        closes.Should().Be(0);
        sut.State.Should().Be(UpdateDialogState.Error);
        sut.Message.Should().Be(UpdateMessages.SKIP_FAILED);
    }

    /// <summary>Verifies that Skip does nothing when no version is offered.</summary>
    [Fact]
    public async Task SkipAsync_WhenNothingOffered_DoesNotSave()
    {

        var saved = new List<string>();
        var sut = CreateSut(Outcome(UpdateCheckStatus.UpToDate), saved);

        await sut.CheckAsync();
        await sut.SkipAsync();

        saved.Should().BeEmpty();
    }

    /// <summary>Verifies that the release-notes link opens the offered release page only while a release is known.</summary>
    [Fact]
    public async Task OpenReleaseNotes_InvokesUrlActionOnlyWhileOffered()
    {

        var opened = new List<Uri>();
        var dependencies = Dependencies(static _ => Task.FromResult(AvailableOutcome(UpdateCheckStatus.Available))) with
        {
            OpenUrl = opened.Add,
        };
        var sut = new UpdateViewModel(dependencies, new ManualTimeProvider(NOW));

        sut.OpenReleaseNotes();
        opened.Should().BeEmpty();

        await sut.CheckAsync();
        sut.OpenReleaseNotes();

        opened.Should().Equal(RELEASE_PAGE);
    }

    /// <summary>Verifies that disposing during a check abandons it without an error state.</summary>
    [Fact]
    public async Task Dispose_DuringCheck_AbandonsCheckWithoutError()
    {

        var pending = new TaskCompletionSource<UpdateCheckOutcome>();
        var sut = new UpdateViewModel(Dependencies(ct => pending.Task.WaitAsync(ct)), new ManualTimeProvider(NOW));

        var check = sut.CheckAsync();
        sut.Dispose();
        await check;

        sut.State.Should().Be(UpdateDialogState.Checking);
        sut.IsBusy.Should().BeFalse();
    }
}
