using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

public sealed partial class CopilotUsageProviderTests
{
    [Fact]
    public async Task GetSnapshotAsync_WhenQuotaFailsAndBillingSucceeds_AttachesAvailableBillingToStaleQuota()
    {
        const string billingJson = """
        {
          "organization": "ColibriAgile",
          "timePeriod": { "year": 2026, "month": 9 },
          "usageItems": [
            {
              "product": "Copilot",
              "sku": "Copilot AI Credits",
              "model": "claude-3.5-sonnet",
              "unitType": "ai-credits",
              "grossQuantity": 42.0,
              "discountQuantity": 0.0,
              "netQuantity": 42.0
            }
          ]
        }
        """;

        const string seatsJson = """
        {
          "total_seats": 1,
          "seats": [
            {
              "assignee": { "login": "testuser", "id": 1 },
              "plan_type": "business"
            }
          ]
        }
        """;

        var handler = new DelegateHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.Contains("copilot_internal", StringComparison.Ordinal) == true)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            if (req.RequestUri?.AbsolutePath.EndsWith("/user", StringComparison.Ordinal) == true)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"login\":\"testuser\"}", Encoding.UTF8, "application/json")
                };

            if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(seatsJson, Encoding.UTF8, "application/json")
                };

            if (req.RequestUri?.AbsolutePath.Contains("/settings/billing", StringComparison.Ordinal) == true)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(billingJson, Encoding.UTF8, "application/json")
                };

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        var tempDir = Path.Combine(Path.GetTempPath(), $"provider_test_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, "sessions", "s1");
        Directory.CreateDirectory(sessionsDir);
        File.WriteAllText(
            Path.Combine(sessionsDir, "workspace.yaml"),
            "repository: ColibriAgile/TokenHound\n"
        );

        var archive = new UsageArchive(tempDir);
        var gate = new CopilotRequestGate(archive);
        var billingClient = new CopilotBillingClient(http, gate);
        var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
        var billingService = new CopilotBillingService(billingClient, resolver, archive, gate, clock);
        var client = new CopilotApiClient(http);

        var discovery = new CopilotCredentialDiscovery(
            new EmptyCredentialStore(),
            configReader: new CopilotConfigReader(Path.Combine(tempDir, "missing")),
            environmentReader: name => name == "COPILOT_GITHUB_TOKEN" ? "gho_test_token" : null,
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));

        using var provider = new CopilotUsageProvider(discovery, client, gate, billingService, clock);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Stale, snapshot.Status);
        Assert.NotNull(snapshot.CopilotBilling);
        Assert.Equal(CopilotBillingState.Available, snapshot.CopilotBilling.State);
        Assert.NotNull(snapshot.CopilotBilling.Usage);
        Assert.Equal(42.0m, snapshot.CopilotBilling.Usage.GrossUsed);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenBillingFails_RetainsGoodQuota()
    {
        var handler = new DelegateHandler(req =>
        {
            // Quota requests succeed
            if (req.RequestUri?.AbsolutePath.Contains("copilot_internal", StringComparison.Ordinal) == true)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(VALID_JSON, Encoding.UTF8, "application/json")
                };

            // Billing/seat requests fail with 403 Forbidden
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });

        using var http = new HttpClient(handler);
        var tempDir = Path.Combine(Path.GetTempPath(), $"provider_test_{Guid.NewGuid():N}");
        var archive = new UsageArchive(tempDir);
        var gate = new CopilotRequestGate(archive);
        var billingClient = new CopilotBillingClient(http, gate);
        var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
        var billingService = new CopilotBillingService(billingClient, resolver, archive, gate, clock);
        var client = new CopilotApiClient(http);

        var discovery = new CopilotCredentialDiscovery(
            new EmptyCredentialStore(),
            configReader: new CopilotConfigReader(Path.Combine(tempDir, "missing")),
            environmentReader: name => name == "COPILOT_GITHUB_TOKEN" ? "gho_test_token" : null,
            ghTokenReader: static _ => ValueTask.FromResult<string?>(null));

        using var provider = new CopilotUsageProvider(discovery, client, gate, billingService, clock);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderStatus.Ok, snapshot.Status);
        Assert.Single(snapshot.LimitWindows);
        Assert.NotNull(snapshot.CopilotBilling);
        Assert.Equal(CopilotBillingState.Unavailable, snapshot.CopilotBilling.State);
        Assert.Equal(CopilotBillingReason.UnknownScope, snapshot.CopilotBilling.Reason);
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
