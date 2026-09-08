using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>Verifies Copilot provider status, quota, overage, and guidance mapping.</summary>
public sealed partial class CopilotUsageProviderTests
{
    private const string VALID_JSON = """
    {
      "quota_reset_date_utc": "2026-10-01T00:00:00Z",
      "quota_snapshots": {
        "chat": { "has_quota": false, "unlimited": true },
        "premium_interactions": {
          "quota_id": "premium_interactions",
          "has_quota": true,
          "unlimited": false,
          "entitlement": 3000,
          "remaining": 252,
          "quota_remaining": 252.6,
          "percent_remaining": 8.4,
          "overage_permitted": true
        }
      }
    }
    """;

    [Fact]
    public async Task GetSnapshotAsync_WhenResponseIsFinite_ReturnsOfficialQuota()
    {
        using var provider = CreateProvider("gho-fixture-token", HttpStatusCode.OK, VALID_JSON);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal("copilot", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
        Assert.Single(snapshot.LimitWindows);
        Assert.Equal(252.6, snapshot.LimitWindows[0].RemainingValue);
        Assert.Equal(0.916, snapshot.LimitWindows[0].UsedFraction!.Value, 3);
        Assert.Equal(DateTimeOffset.Parse("2026-10-01T00:00:00Z"), snapshot.LimitWindows[0].ResetTimeUtc);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenOnlyNoFiniteCategoriesExist_ReturnsUnsupported()
    {
        const string json = """
        {
          "quota_reset_date_utc": "2026-10-01T00:00:00Z",
          "quota_snapshots": { "chat": { "has_quota": false, "unlimited": true } }
        }
        """;
        using var provider = CreateProvider("gho-fixture-token", HttpStatusCode.OK, json);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Unsupported, snapshot.Status);
        Assert.Empty(snapshot.LimitWindows);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenNoCredentialExistsReturnsNeedsAuthGuidance()
    {
        using var provider = CreateProvider(null, HttpStatusCode.OK, VALID_JSON);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Contains("gh auth login", snapshot.ErrorDescription);
        Assert.Contains("copilot login", snapshot.ErrorDescription);
        Assert.Contains("Do not paste a PAT", snapshot.ErrorDescription);
    }

    [Fact]
    public async Task GetSnapshotAsync_When401OccursClearsToNeedsAuthWithoutTokenDisclosure()
    {
        using var provider = CreateProvider("gho-fixture-token", HttpStatusCode.Unauthorized);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.DoesNotContain("gho-fixture-token", snapshot.ErrorDescription);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenPatShaped403OccursUsesAuthGuidance()
    {
        using var provider = CreateProvider("ghp_fixture_pat", HttpStatusCode.Forbidden);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Contains("Do not paste a PAT", snapshot.ErrorDescription);
        Assert.Contains("GH_TOKEN", snapshot.ErrorDescription);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenNonPat403OccursUsesUnsupportedAndOverrideWarning()
    {
        using var provider = CreateProvider("gho_fixture_oauth", HttpStatusCode.Forbidden);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Unsupported, snapshot.Status);
        Assert.Contains("GH_TOKEN", snapshot.ErrorDescription);
        Assert.DoesNotContain("revoked", snapshot.ErrorDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSnapshotAsync_When429RetryAfterIsZeroReturnsFutureStaleDeadline()
    {
        using var provider = CreateProvider("gho-fixture-token", HttpStatusCode.TooManyRequests);
        provider.ResponseHeaders["Retry-After"] = "0";
        var before = DateTimeOffset.UtcNow;

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Stale, snapshot.Status);
        Assert.True(snapshot.ActiveBlock!.IsBlocked);
        Assert.True(snapshot.ActiveBlock.ResetTimeUtc > before);
        Assert.Equal(0, snapshot.ActiveBlock.RetryAfterSeconds);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenSchemaDriftsReturnsStaleWithoutFabricatingZero()
    {
        using var provider = CreateProvider(
            "gho-fixture-token",
            HttpStatusCode.OK,
            "{\"quota_reset_date_utc\":\"2026-10-01T00:00:00Z\",\"quota_snapshots\":{\"premium_interactions\":{\"has_quota\":true}}}");

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Stale, snapshot.Status);
        Assert.Empty(snapshot.LimitWindows);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenQuotaIsExhaustedWithOveragePermittedDoesNotBlock()
    {
        const string json = """
        {
          "quota_reset_date_utc": "2026-10-01T00:00:00Z",
          "quota_snapshots": {
            "premium_interactions": {
              "has_quota": true, "unlimited": false, "entitlement": 10,
              "remaining": 0, "quota_remaining": -0.4, "percent_remaining": 0,
              "overage_permitted": true
            }
          }
        }
        """;
        using var provider = CreateProvider("gho-fixture-token", HttpStatusCode.OK, json);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.False(snapshot.ActiveBlock!.IsBlocked);
    }

    private static ProviderHarness CreateProvider(
        string? token,
        HttpStatusCode status,
        string? responseBody = null)
    {
        var discovery = new CopilotCredentialDiscovery(
            new EmptyCredentialStore(),
            configReader: new CopilotConfigReader(global::System.IO.Path.Combine(
                global::System.IO.Path.GetTempPath(),
                $"missing_copilot_{Guid.NewGuid():N}")),
            environmentReader: name => name == "COPILOT_GITHUB_TOKEN" ? token : null,
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));
        var harness = new ProviderHarness(status, responseBody);
        var archiveDir = global::System.IO.Path.Combine(
            global::System.IO.Path.GetTempPath(),
            $"copilot_archive_{Guid.NewGuid():N}");
        var archive = new TokenHound.Infrastructure.Engine.UsageArchive(archiveDir);
        var gate = new CopilotRequestGate(archive);
        var billingClient = new CopilotBillingClient(harness.HttpClient, gate);
        var resolver = new CopilotBillingContextResolver(billingClient);
        var billingService = new CopilotBillingService(billingClient, resolver, archive, gate);
        var client = new CopilotApiClient(harness.HttpClient);
        harness.Client = client;
        return harness.WithProvider(new CopilotUsageProvider(discovery, client, gate, billingService));
    }

    private sealed class EmptyCredentialStore : ICredentialStore
    {
        public ValueTask<string?> ReadCredentialAsync(
            string target,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<string?>(null);
    }

    private sealed class ProviderHarness : IDisposable
    {
        private readonly HttpStatusCode _status;
        private readonly string? _responseBody;

        public ProviderHarness(HttpStatusCode status, string? responseBody)
        {
            _status = status;
            _responseBody = responseBody;
            Handler = new DelegateHandler(CreateResponse);
            HttpClient = new HttpClient(Handler);
        }

        public DelegateHandler Handler { get; }

        public HttpClient HttpClient { get; }

        public CopilotApiClient? Client { get; set; }

        public Dictionary<string, string> ResponseHeaders { get; } = new();

        public CopilotUsageProvider Provider { get; private set; } = null!;

        public ProviderHarness WithProvider(CopilotUsageProvider provider)
        {
            Provider = provider;
            return this;
        }

        public ValueTask<Snapshot> GetSnapshotAsync(
            CancellationToken cancellationToken = default)
            => Provider.GetSnapshotAsync(cancellationToken);

        public void Dispose()
        {
            Provider.Dispose();
            Client?.Dispose();
            HttpClient.Dispose();
            Handler.Dispose();
        }

        private HttpResponseMessage CreateResponse(HttpRequestMessage _)
        {
            var response = new HttpResponseMessage(_status);

            if (_responseBody is not null)
                response.Content = new StringContent(_responseBody, Encoding.UTF8, "application/json");

            foreach (var header in ResponseHeaders)
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return response;
        }
    }
}
