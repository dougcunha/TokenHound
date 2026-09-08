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

public sealed partial class CopilotBillingServiceHistoricalTests
{
    [Fact]
    public async Task GetBillingStatusAsync_When429MidDownload_RecordsDeadlineAndReturnsRateLimited()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_429_{Guid.NewGuid():N}");
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
                    var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    resp.Headers.TryAddWithoutValidation("Retry-After", "90");

                    return resp;
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var billingClient = new CopilotBillingClient(http, gate);
            using var metricsClient = new CopilotMetricsClient(http, http, gate);
            var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
            var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
            using var service = new CopilotBillingService(billingClient, resolver, archive, gate, clock, metricsClient: metricsClient);

            var quota = new CopilotQuotaResponse
            {
                Login = "testuser",
                CopilotPlan = "business",
                OrganizationLoginList = ["ColibriAgile"]
            };

            var status = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingReason.RateLimited, status.Reason);
            Assert.False(gate.CanDispatch);
            Assert.NotNull(gate.ActiveDeadlineUtc);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_WhenCancelled_PropagatesCancellation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_cancel_{Guid.NewGuid():N}");
        try
        {
            var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var billingClient = new CopilotBillingClient(http, gate);
            using var metricsClient = new CopilotMetricsClient(http, http, gate);
            var resolver = new CopilotBillingContextResolver(billingClient, Path.Combine(tempDir, "sessions"));
            using var service = new CopilotBillingService(billingClient, resolver, archive, gate, metricsClient: metricsClient);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.GetBillingStatusAsync("fixture-token", null, cts.Token)
            );
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private sealed class TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
