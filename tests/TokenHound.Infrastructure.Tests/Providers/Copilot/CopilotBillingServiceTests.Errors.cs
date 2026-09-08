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

public sealed partial class CopilotBillingServiceTests
{
    [Fact]
    public async Task GetBillingStatusAsync_WhenAccessDenied_ReturnsStaleWithCachedUsage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_test_{Guid.NewGuid():N}");
        try
        {
            var shouldDeny = false;
            var handler = new TestHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };

                if (shouldDeny)
                    return new HttpResponseMessage(HttpStatusCode.Forbidden);

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

            await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            shouldDeny = true;
            var status = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Stale, status.State);
            Assert.Equal(CopilotBillingReason.AccessDenied, status.Reason);
            Assert.NotNull(status.Usage);
            Assert.Equal(150.5m, status.Usage.GrossUsed);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_WhenNetworkFails_ReturnsStaleWithCachedUsage()
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
                    throw new HttpRequestException("Network failure simulated");

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

            await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            shouldFail = true;
            var status = await service.GetBillingStatusAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingState.Stale, status.State);
            Assert.Equal(CopilotBillingReason.NetworkFailure, status.Reason);
            Assert.NotNull(status.Usage);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetBillingStatusAsync_WhenPeriodMismatch_ReturnsInvalidData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"service_test_{Guid.NewGuid():N}");
        try
        {
            const string mismatchedJson = """
            {
              "organization": "ColibriAgile",
              "timePeriod": { "year": 2026, "month": 8 },
              "usageItems": []
            }
            """;

            var handler = new TestHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
                    };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(mismatchedJson, Encoding.UTF8, "application/json")
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

            Assert.Equal(CopilotBillingState.Unavailable, status.State);
            Assert.Equal(CopilotBillingReason.InvalidData, status.Reason);
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
