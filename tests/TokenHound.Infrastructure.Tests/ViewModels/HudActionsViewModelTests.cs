using AwesomeAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies action dispatching, concurrency guards, shutdown coordination, and status formulation in <see cref="HudActionsViewModel"/>.
/// </summary>
public sealed class HudActionsViewModelTests
{
    /// <summary>
    /// Verifies that Constructor throws ArgumentNullException when required delegates are null.
    /// </summary>
    [Fact]
    public void Constructor_WhenRequiredArgumentsNull_ThrowsArgumentNullException()
    {

        var act1 = () => new HudActionsViewModel(null!, () => Task.CompletedTask, () => { }, () => { });
        var act2 = () => new HudActionsViewModel(_ => Task.CompletedTask, null!, () => { }, () => { });
        var act3 = () => new HudActionsViewModel(_ => Task.CompletedTask, () => Task.CompletedTask, null!, () => { });
        var act4 = () => new HudActionsViewModel(_ => Task.CompletedTask, () => Task.CompletedTask, () => { }, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that FormulateStatusText returns expected status strings across provider snapshot states.
    /// </summary>
    [Fact]
    public void FormulateStatusText_VariousSnapshots_ReturnsExpectedText()
    {

        HudActionsViewModel.FormulateStatusText(null).Should().Be("No providers available");
        HudActionsViewModel.FormulateStatusText([]).Should().Be("No providers available");

        var allOk = new[] { CreateSnapshot("c", ProviderStatus.Ok), CreateSnapshot("u", ProviderStatus.Ok) };
        HudActionsViewModel.FormulateStatusText(allOk).Should().Be("Refresh completed");

        var noneOk = new[] { CreateSnapshot("c", ProviderStatus.RateLimited), CreateSnapshot("u", ProviderStatus.NeedsAuth) };
        HudActionsViewModel.FormulateStatusText(noneOk).Should().Be("No counters updated. Check provider status.");

        var mixed = new[] { CreateSnapshot("c", ProviderStatus.Ok), CreateSnapshot("u", ProviderStatus.RateLimited) };
        HudActionsViewModel.FormulateStatusText(mixed).Should().Be("Refresh completed. Some providers need attention.");
    }

    /// <summary>
    /// Verifies that RefreshAsync transitions through refreshing state and sets formulated status text on completion.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_StateTransitionsAndCompletion_FormulatesStatus()
    {

        var tcs = new TaskCompletionSource();
        var ct = TestContext.Current.CancellationToken;
        var snapshots = new[] { CreateSnapshot("claude", ProviderStatus.Ok) };

        var viewModel = new HudActionsViewModel(
            _ => tcs.Task,
            () => Task.CompletedTask,
            () => { },
            () => { },
            () => snapshots
        );

        var refreshTask = viewModel.RefreshAsync(ct);

        viewModel.IsRefreshing.Should().BeTrue();
        viewModel.RefreshStatusText.Should().Be("Refreshing usage...");
        viewModel.IsStatusVisible.Should().BeTrue();

        tcs.SetResult();
        await refreshTask;

        viewModel.IsRefreshing.Should().BeFalse();
        viewModel.RefreshStatusText.Should().Be("Refresh completed");
        viewModel.IsStatusVisible.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that concurrent calls to RefreshAsync are deduplicated and invoke the refresh operation only once.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenAlreadyRefreshing_ReturnsImmediatelyWithoutDuplicateDispatch()
    {

        var tcs = new TaskCompletionSource();
        var ct = TestContext.Current.CancellationToken;
        var callCount = 0;

        var viewModel = new HudActionsViewModel(
            _ =>
            {
                Interlocked.Increment(ref callCount);
                return tcs.Task;
            },
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

        var task1 = viewModel.RefreshAsync(ct);
        var task2 = viewModel.RefreshAsync(ct);

        callCount.Should().Be(1);

        tcs.SetResult();
        await Task.WhenAll(task1, task2);

        callCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that RefreshAsync handles exceptions by clearing IsRefreshing and setting concise failure status text.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenExceptionThrown_SetsFailureStatusAndClearsProgress()
    {

        var ct = TestContext.Current.CancellationToken;
        var viewModel = new HudActionsViewModel(
            _ => Task.FromException(new InvalidOperationException("API error")),
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

        await viewModel.RefreshAsync(ct);

        viewModel.IsRefreshing.Should().BeFalse();
        viewModel.RefreshStatusText.Should().Be("Refresh failed.");
    }

    /// <summary>
    /// Verifies that RefreshAsync does not display error when canceled during application closing.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_WhenCanceledDuringClosing_DoesNotSetErrorStatus()
    {

        var ct = TestContext.Current.CancellationToken;
        var tcs = new TaskCompletionSource();

        var viewModel = new HudActionsViewModel(
            _ => tcs.Task,
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

        var refreshTask = viewModel.RefreshAsync(ct);
        _ = viewModel.CloseAsync();

        tcs.SetException(new OperationCanceledException());
        await refreshTask;

        viewModel.RefreshStatusText.Should().BeNull();
        viewModel.IsClosing.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CloseAsync invokes close action once, caches the result task, and prevents future action dispatch.
    /// </summary>
    [Fact]
    public async Task CloseAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions()
    {

        var ct = TestContext.Current.CancellationToken;
        var closeCount = 0;
        var settingsCount = 0;
        var aboutCount = 0;
        var refreshCount = 0;

        var viewModel = new HudActionsViewModel(
            _ =>
            {
                Interlocked.Increment(ref refreshCount);
                return Task.CompletedTask;
            },
            () =>
            {
                Interlocked.Increment(ref closeCount);
                return Task.CompletedTask;
            },
            () => settingsCount++,
            () => aboutCount++
        );

        var close1 = viewModel.CloseAsync();
        var close2 = viewModel.CloseAsync();

        close1.Should().BeSameAs(close2);
        await close1;

        closeCount.Should().Be(1);
        viewModel.IsClosing.Should().BeTrue();

        viewModel.ShowSettings();
        viewModel.ShowAbout();
        await viewModel.RefreshAsync(ct);

        settingsCount.Should().Be(0);
        aboutCount.Should().Be(0);
        refreshCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ShowSettings and ShowAbout invoke their injected delegates when not closing.
    /// </summary>
    [Fact]
    public void ShowSettings_And_ShowAbout_WhenNotClosing_InvokeDelegates()
    {

        var settingsCalled = false;
        var aboutCalled = false;

        var viewModel = new HudActionsViewModel(
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            () => settingsCalled = true,
            () => aboutCalled = true
        );

        viewModel.ShowSettings();
        settingsCalled.Should().BeTrue();

        viewModel.ShowAbout();
        aboutCalled.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that DismissStatus clears status text and updates visibility.
    /// </summary>
    [Fact]
    public async Task DismissStatus_ClearsStatusTextAndVisibility()
    {

        var ct = TestContext.Current.CancellationToken;
        var viewModel = new HudActionsViewModel(
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

        await viewModel.RefreshAsync(ct);
        viewModel.IsStatusVisible.Should().BeTrue();

        viewModel.DismissStatus();

        viewModel.RefreshStatusText.Should().BeNull();
        viewModel.IsStatusVisible.Should().BeFalse();
    }

    private static Snapshot CreateSnapshot(string providerId, ProviderStatus status)
        => new()
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = null
        };
}
