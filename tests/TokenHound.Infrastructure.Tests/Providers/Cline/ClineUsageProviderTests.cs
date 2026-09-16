using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies <see cref="ClineUsageProvider"/> snapshot generation, auth expiry pre-checks, and status mapping.
/// </summary>
public sealed class ClineUsageProviderTests
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 16, 20, 0, 0, TimeSpan.Zero);

    private const string USER_JSON = """{"success":true,"data":{"id":"usr-01TEST","displayName":"Test"}}""";
    private const string BALANCE_JSON = """{"success":true,"data":{"userId":"usr-01TEST","balance":12.5}}""";

    /// <summary>Verifies that ProviderId returns "cline".</summary>
    [Fact]
    public void ProviderId_ReturnsCline()
    {

        using var provider = new ClineUsageProvider();

        provider.ProviderId.Should().Be("cline");
    }

    /// <summary>Verifies that missing credentials return NeedsAuth without network traffic.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCredentialsMissing_ReturnsNeedsAuth()
    {

        var discovery = new ClineCredentialDiscovery("C:\\nonexistent\\providers.json", static _ => null);
        var handler = new CountingHandler(static _ => throw new InvalidOperationException("No HTTP expected"));
        using var client = new ClineAccountClient(handler);
        using var provider = new ClineUsageProvider(discovery, client, EmptyReader());

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ErrorDescription.Should().Be(ClineUsageProvider.NEEDS_AUTH_MESSAGE);
        handler.CallCount.Should().Be(0);
    }

    /// <summary>Verifies that an expired borrowed token returns NeedsAuth without network traffic.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenTokenExpired_ReturnsNeedsAuth()
    {

        const string json = """
            {"version":1,"providers":{"cline":{"settings":{"auth":{"accessToken":"workos:old","expiresAt":1000}}}}}
            """;

        using var tempScope = new TempFileScope(json);
        var discovery = new ClineCredentialDiscovery(tempScope.FilePath, static _ => null);
        var handler = new CountingHandler(static _ => throw new InvalidOperationException("No HTTP expected"));
        using var client = new ClineAccountClient(handler);
        using var provider = new ClineUsageProvider(
            discovery,
            client,
            EmptyReader(),
            new FakeTimeProvider(NOW)
        );

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.ErrorDescription.Should().Be(ClineUsageProvider.TOKEN_EXPIRED_MESSAGE);
        handler.CallCount.Should().Be(0);
    }

    /// <summary>Verifies that a free tier account snapshot carries credits and no invented quota.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenFreeTier_ReturnsOkWithCredits()
    {

        using var provider = CreateProvider(static request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/users/me" => JsonResponse(USER_JSON),
            "/api/v1/users/me/plan" => StatusResponse(HttpStatusCode.NotFound),
            _ => JsonResponse(BALANCE_JSON)
        });

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().BeEmpty();
        snapshot.ClineAccount.Should().NotBeNull();
        snapshot.ClineAccount!.BalanceCredits.Should().Be(12.5);
        snapshot.ClineAccount.HasPassSubscription.Should().BeFalse();
        snapshot.ClineLocal.Should().BeNull();
        snapshot.ActiveBlock.Should().BeNull();
    }

    /// <summary>Verifies that HTTP 401 surfaces the NeedsAuth guidance message.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenUnauthorized_ReturnsNeedsAuth()
    {

        using var provider = CreateProvider(static request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/users/me" => JsonResponse(USER_JSON),
            "/api/v1/users/me/plan" => StatusResponse(HttpStatusCode.NotFound),
            _ => StatusResponse(HttpStatusCode.Unauthorized)
        });

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
    }

    /// <summary>Verifies that HTTP 403 surfaces the access denied state.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenForbidden_ReturnsAccessDenied()
    {

        using var provider = CreateProvider(static request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/users/me" => JsonResponse(USER_JSON),
            "/api/v1/users/me/plan" => StatusResponse(HttpStatusCode.NotFound),
            _ => StatusResponse(HttpStatusCode.Forbidden)
        });

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.AccessDenied);
    }

    /// <summary>Verifies that cancellation tokens are honored and propagated.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCancellationRequested_PropagatesException()
    {

        using var provider = new ClineUsageProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await provider.GetSnapshotAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ClineUsageProvider CreateProvider(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {

        var discovery = new ClineCredentialDiscovery(environmentReader: static _ => "workos:test-jwt");

        return new ClineUsageProvider(
            discovery,
            new ClineAccountClient(new MockHttpMessageHandler(handler)),
            EmptyReader(),
            new FakeTimeProvider(NOW)
        );
    }

    private static ClineLocalSessionReader EmptyReader()
        => new(Path.Combine(Path.GetTempPath(), $"cline_empty_{Guid.NewGuid():N}"));

    private static HttpResponseMessage JsonResponse(string payload)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage StatusResponse(HttpStatusCode status)
        => new(status);

    private sealed class CountingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {

            CallCount++;

            return Task.FromResult(handler(request));
        }
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            => Task.FromResult(handler(req));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TempFileScope : IDisposable
    {
        public TempFileScope(string content)
        {

            var dir = Path.Combine(Path.GetTempPath(), $"cline_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "providers.json");
            File.WriteAllText(FilePath, content, Encoding.UTF8);
        }

        public string FilePath { get; }

        public void Dispose()
        {

            try
            {

                var dir = Path.GetDirectoryName(FilePath);

                if (dir is not null && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception)
            {
            }
        }
    }
}