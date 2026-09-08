using AwesomeAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies that UsageStore bypasses outer rate-limiting and deadline persistence for IRequestGatedUsageProvider.
/// </summary>
public sealed class RequestGatedUsageStoreTests
{
    /// <summary>
    /// Verifies that an IRequestGatedUsageProvider is not skipped by outer rate-limiting in UsageStore.
    /// </summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderIsRequestGated_BypassesOuterRateLimit()
    {
        using var store = new UsageStore();
        var future = DateTimeOffset.UtcNow.AddMinutes(5);

        var provider = Substitute.For<IRequestGatedUsageProvider>();
        provider.ProviderId.Returns("custom-gated");
        var snapshot = CreateSnapshot("custom-gated", ProviderStatus.RateLimited, future);
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);

        // First refresh stores snapshot with ActiveBlock
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        // Second refresh should STILL poll the provider because it is request-gated
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that a normal IUsageProvider is skipped by outer rate-limiting in UsageStore.
    /// </summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderIsNotRequestGated_EnforcesOuterRateLimit()
    {
        using var store = new UsageStore();
        var future = DateTimeOffset.UtcNow.AddMinutes(5);

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("normal-provider");
        var snapshot = CreateSnapshot("normal-provider", ProviderStatus.RateLimited, future);
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);

        // First refresh stores snapshot with ActiveBlock
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        // Second refresh should skip polling because normal provider is rate-limited
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when a refresh throws an exception, CreateErrorSnapshot retains known billing as stale.
    /// </summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderThrows_RetainsKnownBillingAsStale()
    {
        using var store = new UsageStore();
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("billing-provider");

        var billing = new CopilotBillingStatus
        {
            State = CopilotBillingState.Available,
            Reason = CopilotBillingReason.None,
            AttemptedAtUtc = DateTimeOffset.UtcNow
        };

        var initialSnapshot = CreateSnapshot("billing-provider", ProviderStatus.Ok, null) with
        {
            CopilotBilling = billing
        };

        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(
                ValueTask.FromResult(initialSnapshot),
                ValueTask.FromException<Snapshot>(new global::System.Net.Http.HttpRequestException("Network failure"))
            );

        store.RegisterProvider(provider);

        // First refresh succeeds and populates billing
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        store.CurrentSnapshots["billing-provider"].CopilotBilling!.State.Should().Be(CopilotBillingState.Available);

        // Second refresh throws exception; error snapshot should retain billing as stale
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        var current = store.CurrentSnapshots["billing-provider"];
        current.Status.Should().Be(ProviderStatus.Stale);
        current.CopilotBilling.Should().NotBeNull();
        current.CopilotBilling!.State.Should().Be(CopilotBillingState.Stale);
        current.CopilotBilling.Reason.Should().Be(CopilotBillingReason.NetworkFailure);
    }

    private static Snapshot CreateSnapshot(
        string providerId,
        ProviderStatus status,
        DateTimeOffset? resetTime)
    {
        UsageBlock? block = resetTime.HasValue
            ? new UsageBlock
            {
                Reason = "Rate limited",
                IsBlocked = true,
                ResetTimeUtc = resetTime,
                RetryAfterSeconds = 60
            }
            : null;

        return new Snapshot
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = block,
            ErrorDescription = null
        };
    }
}
