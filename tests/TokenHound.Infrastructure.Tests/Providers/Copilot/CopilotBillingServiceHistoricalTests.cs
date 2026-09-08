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
/// Verifies historical metrics report fallback, multi-pass bounded progress, and safety guards in CopilotBillingService.
/// </summary>
public sealed partial class CopilotBillingServiceHistoricalTests
{
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

    private const string MANIFEST_JSON = """
    {
      "report_day": "2026-09-01",
      "download_links": [
        "https://reports.github.com/data/part1.ndjson"
      ]
    }
    """;

    private const string NDJSON_REPORT = """
    {"day":"2026-09-01","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":10.5}
    {"day":"2026-09-01","user_id":2,"user_login":"bob","organization_id":"ColibriAgile","ai_credits_used":4.5}
    """;

    [Fact]
    public async Task GetBillingStatusAsync_WhenDirectBillingNotFound_FallsBackToDailyReports()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_hist_{Guid.NewGuid():N}");
        try
        {
            var handler = new TestHandler(req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/seats", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains("/settings/billing/ai_credit/usage", StringComparison.Ordinal))
                    return new HttpResponseMessage(HttpStatusCode.NotFound);

                if (path.Contains("/metrics/reports/users-1-day", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(MANIFEST_JSON, Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains("/data/part1.ndjson", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(NDJSON_REPORT, Encoding.UTF8, "application/octet-stream")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var billingClient = new CopilotBillingClient(http, gate);
            using var metricsClient = new CopilotMetricsClient(http, http, gate);
            var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
            using var service = new CopilotBillingService(billingClient, resolver, archive, gate, clock, metricsClient: metricsClient);

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
            Assert.Equal(CopilotCreditSource.DailyUserReport, status.Usage.Source);
            Assert.True(status.Usage.IsEstimated);
            Assert.Equal(15.0m, status.Usage.GrossUsed);
            Assert.Null(status.Usage.IncludedTotal);
            Assert.Null(status.Usage.Remaining);

            var summaries = archive.LoadCopilotDailySummaries(status.Usage.Context, status.Usage.Period);
            Assert.NotEmpty(summaries);
            Assert.Equal(new DateOnly(2026, 9, 1), summaries[0].Day);
            Assert.Equal(15.0m, summaries[0].AiCreditsUsed);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_MultiPassPartitionProgress_ResumesUnfinishedPartitions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_multipass_{Guid.NewGuid():N}");
        try
        {
            const string threePartManifest = """
            {
              "report_day": "2026-09-01",
              "download_links": [
                "https://reports.github.com/data/part1.ndjson",
                "https://reports.github.com/data/part2.ndjson",
                "https://reports.github.com/data/part3.ndjson"
              ]
            }
            """;

            var handler = new TestHandler(req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/seats", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains("/settings/billing/ai_credit/usage", StringComparison.Ordinal))
                    return new HttpResponseMessage(HttpStatusCode.NotFound);

                if (path.Contains("/metrics/reports/users-1-day", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(threePartManifest, Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains("/data/part1.ndjson", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"day\":\"2026-09-01\",\"user_id\":1,\"user_login\":\"alice\",\"ai_credits_used\":10.0}", Encoding.UTF8, "application/octet-stream")
                    };
                }

                if (path.Contains("/data/part2.ndjson", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"day\":\"2026-09-01\",\"user_id\":2,\"user_login\":\"bob\",\"ai_credits_used\":15.0}", Encoding.UTF8, "application/octet-stream")
                    };
                }

                if (path.Contains("/data/part3.ndjson", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"day\":\"2026-09-01\",\"user_id\":3,\"user_login\":\"carol\",\"ai_credits_used\":5.0}", Encoding.UTF8, "application/octet-stream")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var billingClient = new CopilotBillingClient(http, gate);
            using var metricsClient = new CopilotMetricsClient(http, http, gate);
            var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
            using var service = new CopilotBillingService(billingClient, resolver, archive, gate, clock, metricsClient: metricsClient);

            var quota = new CopilotQuotaResponse
            {
                Login = "testuser",
                CopilotPlan = "business",
                OrganizationLoginList = ["ColibriAgile"]
            };

            // Pass 1: seats(1) + billing probe(1) + manifest(1) + part1(1) = 4 dispatches!
            // Budget exhausted before part2!
            var status1 = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            // Day should not be committed yet because part2 is pending
            var context = new CopilotBillingContext
            {
                PrincipalId = "testuser",
                Scope = CopilotBillingScope.Organization,
                OwnerId = "ColibriAgile",
                Plan = CopilotPlanType.Business
            };
            var period = new CopilotBillingPeriod { RequestedYear = 2026, RequestedMonth = 9 };
            var summariesBefore = archive.LoadCopilotDailySummaries(context, period);
            Assert.Empty(summariesBefore);

            // Pass 2: part2 is downloaded and day is completed!
            var status2 = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Available, status2.State);
            Assert.NotNull(status2.Usage);
            Assert.Equal(30.0m, status2.Usage.GrossUsed);

            var summariesAfter = archive.LoadCopilotDailySummaries(context, period);
            Assert.Single(summariesAfter);
            Assert.Equal(30.0m, summariesAfter[0].AiCreditsUsed);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
