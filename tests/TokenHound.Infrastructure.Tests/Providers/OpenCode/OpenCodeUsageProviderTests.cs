using AwesomeAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Providers.OpenCode;
using TokenHound.Infrastructure.Tests.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.OpenCode;

/// <summary>
/// Unit tests verifying <see cref="OpenCodeUsageProvider"/> snapshot generation, window mapping, and status transitions.
/// </summary>
public sealed class OpenCodeUsageProviderTests
{
    private const string SUCCESS_JSON = """{"usage":{"rolling":{"status":"ok","percent":12.5,"resetsAt":"2026-09-12T00:45:07.613Z"},"weekly":{"status":"ok","percent":34.0,"resetsAt":"2026-09-14T00:00:00.613Z"},"monthly":{"status":"ok","percent":58.2,"resetsAt":"2026-10-10T14:55:16.613Z"}}}""";

    /// <summary>Verifies that ProviderId returns "opencode".</summary>
    [Fact]
    public void ProviderId_ReturnsOpenCode()
    {

        using var provider = new OpenCodeUsageProvider();

        provider.ProviderId.Should().Be("opencode");
    }

    /// <summary>Verifies that missing credentials return NeedsAuth status.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCredentialsMissing_ReturnsNeedsAuth()
    {

        var discovery = new OpenCodeCredentialDiscovery("C:\\nonexistent\\auth.json", static _ => null);
        using var provider = new OpenCodeUsageProvider(discovery);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ErrorDescription.Should().Be(OpenCodeUsageProvider.NEEDS_AUTH_MESSAGE);
    }

    /// <summary>Verifies that successful API responses map 3 limit windows with accurate properties.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenSuccessful_ReturnsOkWithThreeLimitWindows()
    {

        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "zen_test_key");
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json")
        });
        using var client = new OpenCodeApiClient(handler);
        using var provider = new OpenCodeUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().HaveCount(3);
        snapshot.LimitWindows[0].Name.Should().Be("5-Hour Rolling");
        snapshot.LimitWindows[0].Period.Should().Be(TimeSpan.FromHours(5));
        snapshot.LimitWindows[0].UsedFraction.Should().Be(0.125);
        snapshot.LimitWindows[1].Name.Should().Be("Weekly");
        snapshot.LimitWindows[1].UsedFraction.Should().Be(0.34);
        snapshot.LimitWindows[2].Name.Should().Be("Monthly");
        snapshot.LimitWindows[2].UsedFraction!.Value.Should().BeApproximately(0.582, 1e-4);
    }

    /// <summary>Verifies that a successful rate-limited payload creates a reset-based polling block.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenSuccessfulPayloadIsRateLimited_UsesResetDeadlineAndBlocksPolling()
    {

        var nowUtc = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var resetUtc = new DateTimeOffset(2026, 9, 12, 10, 2, 30, 500, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(nowUtc);
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var calls = 0;
        var handler = new MockHttpMessageHandler(_ => ++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"usage":{"rolling":{"status":"rate-limited","percent":100.0,"resetsAt":"2026-09-12T10:02:30.500Z"},"weekly":{"status":"ok","percent":34.0,"resetsAt":"2026-09-14T00:00:00Z"},"monthly":{"status":"ok","percent":58.2,"resetsAt":"2026-10-10T14:55:16Z"}}}""",
                    Encoding.UTF8,
                    "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json")
            });
        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        using var provider = new OpenCodeUsageProvider(discovery, client, timeProvider);

        var limited = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        limited.Status.Should().Be(ProviderStatus.RateLimited);
        limited.ActiveBlock.Should().NotBeNull();
        limited.ActiveBlock!.IsBlocked.Should().BeTrue();
        limited.ActiveBlock.ResetTimeUtc.Should().Be(resetUtc);
        calls.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromMinutes(2));
        var lockedOut = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        lockedOut.Status.Should().Be(ProviderStatus.RateLimited);
        calls.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromSeconds(30.5));
        var recovered = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        recovered.Status.Should().Be(ProviderStatus.Ok);
        calls.Should().Be(2);
    }

    /// <summary>Verifies that percentages outside 0-100 clamp UsedFraction appropriately.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenExtremePercentages_ClampsUsedFraction()
    {

        const string json = """{"usage":{"rolling":{"status":"ok","percent":150.0,"resetsAt":"2026-09-12T00:00:00Z"},"weekly":{"status":"ok","percent":-10.0,"resetsAt":"2026-09-14T00:00:00Z"},"monthly":{"status":"ok","percent":0.0,"resetsAt":"2026-10-10T00:00:00Z"}}}""";
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        using var client = new OpenCodeApiClient(handler);
        using var provider = new OpenCodeUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows[0].UsedFraction.Should().Be(1.0);
        snapshot.LimitWindows[0].RemainingUnits.Should().Be(0L);
        snapshot.LimitWindows[1].UsedFraction.Should().Be(0.0);
        snapshot.LimitWindows[2].UsedFraction.Should().Be(0.0);
        snapshot.LimitWindows[2].RemainingUnits.Should().Be(100L);
    }

    /// <summary>Verifies that HTTP 401 and 403 transition to NeedsAuth and AccessDenied respectively.</summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ProviderStatus.NeedsAuth, null)]
    [InlineData(HttpStatusCode.Forbidden, ProviderStatus.AccessDenied, OpenCodeUsageProvider.ACCESS_DENIED_MESSAGE)]
    public async Task GetSnapshotAsync_WhenAuthOrForbidden_MapsExpectedStatus(
        HttpStatusCode code,
        ProviderStatus expectedStatus,
        string? expectedError)
    {

        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(code)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
        using var client = new OpenCodeApiClient(handler);
        using var provider = new OpenCodeUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(expectedStatus);
        snapshot.LimitWindows.Should().BeEmpty();

        if (expectedError is not null)
            snapshot.ErrorDescription.Should().Be(expectedError);
    }

    /// <summary>Verifies that HTTP 429 sets ActiveBlock with RateLimitReached reason and respects Retry-After.</summary>
    [Theory]
    [InlineData("120", 120)]
    [InlineData("0", 0)]
    public async Task GetSnapshotAsync_WhenApiReturns429_SetsActiveBlockAndEnforcesFloor(
        string header,
        int expectedSeconds)
    {

        var nowUtc = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(nowUtc);
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(_ => CreateRateLimitResponse(header));
        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        using var provider = new OpenCodeUsageProvider(discovery, client, timeProvider);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock.Should().NotBeNull();
        snapshot.ActiveBlock!.IsBlocked.Should().BeTrue();
        snapshot.ActiveBlock.Reason.Should().Be(OpenCodeUsageProvider.RATE_LIMIT_REASON);
        snapshot.ActiveBlock.RetryAfterSeconds.Should().Be(expectedSeconds);
        snapshot.ActiveBlock.ResetTimeUtc.Should().BeOnOrAfter(nowUtc + RateLimitPolicy.MINIMUM_RETRY_FLOOR);
    }

    /// <summary>Verifies that a no-header 429 uses a valid server reset without a cached snapshot.</summary>
    [Fact]
    public async Task GetSnapshotAsync_When429HasServerResetWithoutHeader_UsesServerResetAndLocksOutPolling()
    {

        var nowUtc = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var resetTimeUtc = nowUtc.AddMinutes(5);
        var timeProvider = new MutableTimeProvider(nowUtc);
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var calls = 0;
        var handler = new MockHttpMessageHandler(_ => ++calls == 1
            ? CreateRateLimitResponse(null, resetTimeUtc.ToString("O"))
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json")
            });
        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        using var provider = new OpenCodeUsageProvider(discovery, client, timeProvider);

        var limited = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        limited.Status.Should().Be(ProviderStatus.RateLimited);
        limited.ActiveBlock!.ResetTimeUtc.Should().Be(resetTimeUtc);
        calls.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromMinutes(4));
        var lockedOut = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        lockedOut.Status.Should().Be(ProviderStatus.RateLimited);
        calls.Should().Be(1);
    }

    /// <summary>Verifies that missing, malformed, or past reset data falls back to the policy floor.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("not-a-timestamp")]
    [InlineData("2026-09-12T09:59:00Z")]
    public async Task GetSnapshotAsync_When429ResetDataIsInvalid_FallsBackToPolicyFloor(string? resetTime)
    {

        var nowUtc = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(nowUtc);
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(_ => CreateRateLimitResponse(null, resetTime));
        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        using var provider = new OpenCodeUsageProvider(discovery, client, timeProvider);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock!.ResetTimeUtc.Should().BeOnOrAfter(nowUtc + RateLimitPolicy.MINIMUM_RETRY_FLOOR);
        snapshot.ActiveBlock.RetryAfterSeconds.Should().BeNull();
    }

    /// <summary>Verifies that active rate limit locks out polling until deadline expires.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenRateLimited_LocksOutPollingUntilDeadline()
    {

        var nowUtc = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(nowUtc);
        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var calls = 0;
        var handler = new MockHttpMessageHandler(_ => ++calls == 1
            ? CreateRateLimitResponse("100")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json") });
        using var client = new OpenCodeApiClient(handler, timeProvider: timeProvider);
        using var provider = new OpenCodeUsageProvider(discovery, client, timeProvider);

        var initial = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        initial.Status.Should().Be(ProviderStatus.RateLimited);

        timeProvider.Advance(TimeSpan.FromSeconds(50));
        var lockedOut = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        lockedOut.Status.Should().Be(ProviderStatus.RateLimited);
        calls.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromSeconds(60));
        var recovered = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        recovered.Status.Should().Be(ProviderStatus.Ok);
        calls.Should().Be(2);
    }

    /// <summary>Verifies that network errors after a successful reading return Stale with cached windows.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenNetworkFails_ReturnsStalePreservingCachedWindows()
    {

        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var succeed = true;
        var handler = new MockHttpMessageHandler(_ => succeed
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json") }
            : throw new HttpRequestException("Socket failure"));
        using var client = new OpenCodeApiClient(handler);
        using var provider = new OpenCodeUsageProvider(discovery, client);

        var okSnapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        okSnapshot.Status.Should().Be(ProviderStatus.Ok);
        okSnapshot.LimitWindows.Should().HaveCount(3);

        succeed = false;
        var stale = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        stale.Status.Should().Be(ProviderStatus.Stale);
        stale.LimitWindows.Should().HaveCount(3);
        stale.LimitWindows[0].Name.Should().Be("5-Hour Rolling");
    }

    /// <summary>Verifies that initial network failure returns Stale with empty limit windows.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenInitialNetworkFails_ReturnsStaleWithEmptyWindows()
    {

        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(static _ => throw new HttpRequestException("Network down"));
        using var client = new OpenCodeApiClient(handler);
        using var provider = new OpenCodeUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Stale);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ErrorDescription.Should().Contain("Network down");
    }

    /// <summary>Verifies that cancellation tokens are honored and propagated.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCancellationRequested_PropagatesException()
    {

        using var provider = new OpenCodeUsageProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await provider.GetSnapshotAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>Verifies that injecting a client factory instantiates the client on demand.</summary>
    [Fact]
    public async Task Constructor_WithClientFactory_UsesFactoryClient()
    {

        var discovery = new OpenCodeCredentialDiscovery(environmentReader: static _ => "key");
        var handler = new MockHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SUCCESS_JSON, Encoding.UTF8, "application/json")
        });
        var factoryCalled = false;
        using var provider = new OpenCodeUsageProvider(
            discovery,
            null,
            clientFactory: () =>
            {

                factoryCalled = true;

                return new OpenCodeApiClient(handler);
            }
        );

        factoryCalled.Should().BeTrue();
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        snapshot.Status.Should().Be(ProviderStatus.Ok);
    }

    private static HttpResponseMessage CreateRateLimitResponse(string? retryAfter, string? resetTimeUtc = null)
    {

        var payload = resetTimeUtc is null
            ? """{"type":"error","error":{"type":"GoUsageLimitError","message":"Limit hit"}}"""
            : JsonSerializer.Serialize(new
            {
                type = "error",
                resetsAt = resetTimeUtc,
                error = new { type = "GoUsageLimitError", message = "Limit hit" }
            });
        var res = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (retryAfter is not null)
            res.Headers.Add("Retry-After", retryAfter);

        return res;
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            => Task.FromResult(handler(req));
    }
}
