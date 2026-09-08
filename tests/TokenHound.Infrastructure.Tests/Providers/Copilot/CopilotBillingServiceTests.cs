using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies direct billing aggregation, cache retention, error states, and rate limiting for CopilotBillingService.
/// </summary>
public sealed partial class CopilotBillingServiceTests
{
    private const string BILLING_JSON = """
    {
      "organization": "ColibriAgile",
      "timePeriod": {
        "year": 2026,
        "month": 9
      },
      "usageItems": [
        {
          "product": "Copilot",
          "sku": "Copilot AI Credits",
          "model": "claude-3.5-sonnet",
          "unitType": "ai-credits",
          "grossQuantity": 100.5,
          "discountQuantity": 20.0,
          "netQuantity": 80.5
        },
        {
          "product": "Copilot",
          "sku": "Copilot AI Credits",
          "model": "gpt-4o",
          "unitType": "ai-credits",
          "grossQuantity": 50.0,
          "discountQuantity": 0.0,
          "netQuantity": 50.0
        }
      ]
    }
    """;

    private const string SEATS_JSON = """
    {
      "total_seats": 1,
      "seats": [
        {
          "assignee": { "login": "testuser", "id": 1 },
          "plan_type": "business",
          "created_at": "2026-01-01T00:00:00Z"
        }
      ]
    }
    """;

    [Fact]
    public async Task GetBillingStatusAsync_WhenBillingSucceeds_AggregatesAndSavesToArchive()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_test_{Guid.NewGuid():N}");
        try
        {
            var handler = new TestHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BILLING_JSON, Encoding.UTF8, "application/json")
                };
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var client = new CopilotBillingClient(http, gate);
            var resolver = new CopilotBillingContextResolver(client, Path.Combine(tempDir, "sessions"));
            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
            using var service = new CopilotBillingService(client, resolver, archive, gate, clock);

            var quota = new CopilotQuotaResponse
            {
                Login = "testuser",
                CopilotPlan = "business",
                OrganizationLoginList = ["ColibriAgile"]
            };

            var status = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Available, status.State);
            Assert.Equal(CopilotBillingReason.None, status.Reason);
            Assert.NotNull(status.Usage);
            Assert.Equal(150.5m, status.Usage.GrossUsed);
            Assert.Equal(20.0m, status.Usage.DiscountedUsed);
            Assert.Equal(130.5m, status.Usage.NetUsed);
            Assert.Null(status.Usage.IncludedTotal);
            Assert.Null(status.Usage.Remaining);

            // Verify persistence in archive
            var cached = archive.LoadCopilotBilling(
                "testuser",
                CopilotBillingScope.Organization,
                "ColibriAgile",
                2026,
                9
            );
            Assert.NotNull(cached);
            Assert.Equal(150.5m, cached.GrossUsed);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_WhenUnresolvedScope_ReturnsUnavailableWithUnknownScope()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_test_{Guid.NewGuid():N}");
        try
        {
            var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var client = new CopilotBillingClient(http, gate);
            var resolver = new CopilotBillingContextResolver(client, Path.Combine(tempDir, "sessions"));
            using var service = new CopilotBillingService(client, resolver, archive, gate);

            var quota = new CopilotQuotaResponse
            {
                Login = "testuser",
                CopilotPlan = "business",
                OrganizationLoginList = []
            };

            var status = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Unavailable, status.State);
            Assert.Equal(CopilotBillingReason.UnknownScope, status.Reason);
            Assert.Null(status.Usage);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_WhenRateLimited_ReturnsStaleWithCachedUsage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_test_{Guid.NewGuid():N}");
        try
        {
            var shouldFail = false;
            var handler = new TestHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };

                if (shouldFail)
                {
                    var rateLimitResp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    rateLimitResp.Headers.TryAddWithoutValidation("Retry-After", "60");
                    return rateLimitResp;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BILLING_JSON, Encoding.UTF8, "application/json")
                };
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var client = new CopilotBillingClient(http, gate);
            var resolver = new CopilotBillingContextResolver(client, Path.Combine(tempDir, "sessions"));
            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
            using var service = new CopilotBillingService(client, resolver, archive, gate, clock);

            var quota = new CopilotQuotaResponse
            {
                Login = "testuser",
                CopilotPlan = "business",
                OrganizationLoginList = ["ColibriAgile"]
            };

            // First call succeeds
            var initialStatus = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);
            Assert.Equal(CopilotBillingState.Available, initialStatus.State);

            // Second call encounters 429
            shouldFail = true;
            var secondStatus = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Stale, secondStatus.State);
            Assert.Equal(CopilotBillingReason.RateLimited, secondStatus.Reason);
            Assert.NotNull(secondStatus.Usage);
            Assert.Equal(150.5m, secondStatus.Usage.GrossUsed);
            Assert.NotNull(secondStatus.NextRequestAtUtc);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
