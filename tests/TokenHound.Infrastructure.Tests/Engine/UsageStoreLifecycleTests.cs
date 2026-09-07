using AwesomeAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>Verifies terminal stop, admission closing, draining, and lifecycle safety in UsageStore.</summary>
public sealed class UsageStoreLifecycleTests
{
    /// <summary>Verifies that Dispose stops the periodic polling timer cleanly.</summary>
    [Fact]
    public void Start_And_Dispose_StopsTimerCleanly()
    {

        var store = new UsageStore();

        store.Start(TimeSpan.FromMilliseconds(50));
        store.IsRunning.Should().BeTrue();

        store.Dispose();
        store.IsRunning.Should().BeFalse();
    }

    /// <summary>Verifies that Start with short poll interval executes background ticks and updates snapshots.</summary>
    [Fact]
    public async Task Start_WithShortPollInterval_ExecutesBackgroundTicks()
    {

        using var store = new UsageStore();
        var tcs = new TaskCompletionSource<Snapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = CreateSnapshot("claude");

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(expected));

        store.SnapshotUpdated += (_, s) => tcs.TrySetResult(s);
        store.RegisterProvider(provider);

        store.Start(TimeSpan.FromMilliseconds(20));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var received = await tcs.Task;
        received.Should().Be(expected);
    }

    /// <summary>Verifies that Stop is restartable and does not close admission permanently.</summary>
    [Fact]
    public async Task Stop_ThenStart_IsRestartable()
    {

        using var store = new UsageStore();
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(CreateSnapshot("claude")));

        store.RegisterProvider(provider);
        store.Start(TimeSpan.FromMinutes(1));
        store.IsRunning.Should().BeTrue();

        store.Stop();
        store.IsRunning.Should().BeFalse();

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        store.Start(TimeSpan.FromMinutes(1));
        store.IsRunning.Should().BeTrue();
    }

    /// <summary>Verifies that terminal StopAsync closes admission and rejects new operations.</summary>
    [Fact]
    public async Task StopAsync_ClosesAdmission_NewOperationsThrowObjectDisposedException()
    {

        var store = new UsageStore();
        await store.StopAsync(TestContext.Current.CancellationToken);

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");

        var actRefresh = async () => await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var actTick = async () => await store.TickAsync(TestContext.Current.CancellationToken);
        var actStart = () => store.Start(TimeSpan.FromSeconds(1));
        var actRegister = () => store.RegisterProvider(provider);

        await actRefresh.Should().ThrowAsync<ObjectDisposedException>();
        await actTick.Should().ThrowAsync<ObjectDisposedException>();
        actStart.Should().Throw<ObjectDisposedException>();
        actRegister.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>Verifies that StopAsync cancels active provider refreshes and drains before completing.</summary>
    [Fact]
    public async Task StopAsync_CancelsActiveRefresh_AndDrainsBeforeResourceDisposal()
    {

        var store = new UsageStore();
        var startedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<Snapshot> HangUntilCanceledAsync(CancellationToken ct)
        {

            startedTcs.TrySetResult();

            using var reg = ct.Register(() => canceledTcs.TrySetResult());
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);

            return CreateSnapshot("claude");
        }

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(callInfo => HangUntilCanceledAsync(callInfo.Arg<CancellationToken>()));

        store.RegisterProvider(provider);
        var refreshTask = store.RefreshNowAsync(CancellationToken.None);

        await startedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        var stopTask = store.StopAsync(TestContext.Current.CancellationToken);

        await canceledTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await stopTask.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        var actRefresh = async () => await refreshTask;
        await actRefresh.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that refreshes queued on the semaphore are canceled and drained on StopAsync.</summary>
    [Fact]
    public async Task StopAsync_WaitingRefreshInSemaphore_IsCanceledAndDrained()
    {

        var store = new UsageStore();
        var firstEnteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<Snapshot> HangUntilCanceledAsync(CancellationToken ct)
        {

            firstEnteredTcs.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);

            return CreateSnapshot("claude");
        }

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(callInfo => HangUntilCanceledAsync(callInfo.Arg<CancellationToken>()));

        store.RegisterProvider(provider);

        var task1 = store.RefreshNowAsync(CancellationToken.None);
        await firstEnteredTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        var task2 = store.RefreshNowAsync(CancellationToken.None);

        await store.StopAsync(TestContext.Current.CancellationToken);

        var act1 = async () => await task1;
        var act2 = async () => await task2;

        await act1.Should().ThrowAsync<OperationCanceledException>();
        await act2.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that concurrent StopAsync callers observe the same completion task.</summary>
    [Fact]
    public async Task StopAsync_ConcurrentCalls_ObserveSameCompletion()
    {

        var store = new UsageStore();
        var task1 = store.StopAsync(TestContext.Current.CancellationToken);
        var task2 = store.StopAsync(TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        task1.IsCompletedSuccessfully.Should().BeTrue();
        task2.IsCompletedSuccessfully.Should().BeTrue();
    }

    /// <summary>Verifies that synchronous Dispose requests terminal stop without blocking.</summary>
    [Fact]
    public async Task Dispose_RequestsTerminalStop_AndDrainsWithoutBlocking()
    {

        var store = new UsageStore();
        var startedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<Snapshot> HangUntilCanceledAsync(CancellationToken ct)
        {

            startedTcs.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);

            return CreateSnapshot("claude");
        }

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(callInfo => HangUntilCanceledAsync(callInfo.Arg<CancellationToken>()));

        store.RegisterProvider(provider);
        var refreshTask = store.RefreshNowAsync(CancellationToken.None);

        await startedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        store.Dispose();

        var actRefresh = async () => await refreshTask;
        await actRefresh.Should().ThrowAsync<OperationCanceledException>();

        var actNewRefresh = async () => await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await actNewRefresh.Should().ThrowAsync<ObjectDisposedException>();
    }

    private static Snapshot CreateSnapshot(string providerId)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = null
        };
}
