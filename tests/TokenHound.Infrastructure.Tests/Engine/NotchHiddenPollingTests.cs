using AwesomeAssertions;
using NSubstitute;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.UI.Tray;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies that Notch hide does not affect background polling in <see cref="UsageStore"/> (TC-11).
/// </summary>
public sealed class NotchHiddenPollingTests
{
    [ModuleInitializer]
    internal static void InitializePackUriScheme()
    {

        if (!UriParser.IsKnownScheme("pack"))
            UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
    }

    /// <summary>
    /// Verifies that <see cref="NotchVisibilityController"/> holds no reference to <see cref="UsageStore"/> (TC-11).
    /// </summary>
    [Fact]
    public void Controller_HoldsNoUsageStoreReference_ConfirmingDecoupling()
    {

        var controllerType = typeof(NotchVisibilityController);
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        var fields = controllerType.GetFields(FLAGS);
        var properties = controllerType.GetProperties(FLAGS);

        fields.Should().NotContain(f => f.FieldType == typeof(UsageStore));
        properties.Should().NotContain(p => p.PropertyType == typeof(UsageStore));
    }

    /// <summary>
    /// Verifies that hiding the Notch does not interrupt or pause <see cref="UsageStore"/> background polling,
    /// and that at least two poll ticks are recorded while hidden (TC-11).
    /// </summary>
    [Fact]
    public async Task Hide_DoesNotInterruptUsageStorePolling_RecordsTicksWhileHidden()
    {

        using var store = new UsageStore(
            idleInterval: TimeSpan.FromMilliseconds(20),
            pollInterval: TimeSpan.FromMilliseconds(20)
        );
        var fakeSnapshot = CreateSnapshot("fake-provider");
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("fake-provider");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(fakeSnapshot));

        store.RegisterProvider(provider);

        var controller = new NotchVisibilityController(static () => { }, static () => { }, initiallyVisible: true);
        var ticksCompleted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ticksAfterHide = 0;

        store.SnapshotUpdated += (_, _) =>
        {

            if (!controller.IsNotchVisible && Interlocked.Increment(ref ticksAfterHide) >= 2)
                ticksCompleted.TrySetResult(ticksAfterHide);
        };

        store.Start(TimeSpan.FromMilliseconds(20));
        controller.Hide();
        controller.IsNotchVisible.Should().BeFalse();

        var recordedTicks = await ticksCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        recordedTicks.Should().BeGreaterThanOrEqualTo(2);
        store.IsRunning.Should().BeTrue();
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
