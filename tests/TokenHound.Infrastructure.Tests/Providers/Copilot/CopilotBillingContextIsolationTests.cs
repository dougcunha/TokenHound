using System;
using System.Collections.Generic;
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
/// Verifies that resumable billing work and endpoint capabilities are isolated by billing identity.
/// </summary>
public sealed class CopilotBillingContextIsolationTests
{
    /// <summary>
    /// Verifies interleaved contexts resume only their own report and probe capabilities independently.
    /// </summary>
    [Fact]
    public async Task GetBillingStatusAsync_WhenContextsInterleave_IsolatesProgressAndCapability()
    {

        using var harness = new BillingHarness();

        var firstA = await harness.GetStatusAsync("alice", "OrgA");
        var firstB = await harness.GetStatusAsync("bob", "OrgB");

        Assert.Null(firstA.Usage);
        Assert.Null(firstB.Usage);
        Assert.Equal(1, harness.GetDirectRequestCount("OrgA"));
        Assert.Equal(1, harness.GetDirectRequestCount("OrgB"));

        var completedA = await harness.GetStatusAsync("alice", "OrgA");
        var completedB = await harness.GetStatusAsync("bob", "OrgB");

        Assert.Equal(60m, completedA.Usage?.GrossUsed);
        Assert.Equal("OrgA", completedA.Usage?.Context.OwnerId);
        Assert.Equal(600m, completedB.Usage?.GrossUsed);
        Assert.Equal("OrgB", completedB.Usage?.Context.OwnerId);
    }

    private sealed class BillingHarness : IDisposable
    {
        private readonly string _directory;
        private readonly HttpClient _httpClient;
        private readonly CopilotBillingClient _billingClient;
        private readonly CopilotMetricsClient _metricsClient;
        private readonly CopilotBillingService _service;
        private readonly Dictionary<string, int> _directRequests = new(StringComparer.OrdinalIgnoreCase);

        internal BillingHarness()
        {

            _directory = Path.Combine(Path.GetTempPath(), $"copilot_context_{Guid.NewGuid():N}");
            var archive = new UsageArchive(_directory);
            var gate = new CopilotRequestGate(archive);
            _httpClient = new HttpClient(new RoutingHandler(HandleRequest));
            _billingClient = new CopilotBillingClient(_httpClient, gate);
            _metricsClient = new CopilotMetricsClient(_httpClient, _httpClient, gate);
            var resolver = new CopilotBillingContextResolver(
                _billingClient,
                Path.Combine(_directory, "sessions")
            );
            _service = new CopilotBillingService(
                _billingClient,
                resolver,
                archive,
                gate,
                new MutableTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero)),
                metricsClient: _metricsClient
            );
        }

        internal Task<CopilotBillingStatus> GetStatusAsync(string principal, string owner)
            => _service.GetBillingStatusAsync(
                "fixture-token",
                new CopilotQuotaResponse
                {
                    Login = principal,
                    CopilotPlan = "business",
                    OrganizationLoginList = [owner]
                },
                TestContext.Current.CancellationToken
            );

        internal int GetDirectRequestCount(string owner)
            => _directRequests.GetValueOrDefault(owner);

        /// <inheritdoc />
        public void Dispose()
        {

            _service.Dispose();
            _metricsClient.Dispose();
            _billingClient.Dispose();
            _httpClient.Dispose();

            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private HttpResponseMessage HandleRequest(HttpRequestMessage request)
        {

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.Contains("/copilot/billing/seats", StringComparison.Ordinal))
                return CreateSeatsResponse(ResolveOwner(path));

            if (path.Contains("/settings/billing/ai_credit/usage", StringComparison.Ordinal))
                return CreateDirectResponse(ResolveOwner(path));

            if (path.Contains("/metrics/reports/users-1-day", StringComparison.Ordinal))
                return CreateManifestResponse(ResolveOwner(path));

            if (path.Contains("/data/", StringComparison.Ordinal))
                return CreatePartitionResponse(path);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private HttpResponseMessage CreateDirectResponse(string owner)
        {

            _directRequests[owner] = GetDirectRequestCount(owner) + 1;

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static HttpResponseMessage CreateSeatsResponse(string owner)
    {

        var principal = string.Equals(owner, "OrgA", StringComparison.Ordinal) ? "alice" : "bob";
        var json = $$"""
            {"total_seats":1,"seats":[{"assignee":{"login":"{{principal}}","id":1},"plan_type":"business"}]}
            """;

        return CreateJsonResponse(json);
    }

    private static HttpResponseMessage CreateManifestResponse(string owner)
    {

        var key = owner.ToLowerInvariant();
        var json = $$"""
            {"report_day":"2026-09-01","download_links":["https://reports.github.com/data/{{key}}-part1.ndjson","https://reports.github.com/data/{{key}}-part2.ndjson","https://reports.github.com/data/{{key}}-part3.ndjson"]}
            """;

        return CreateJsonResponse(json);
    }

    private static HttpResponseMessage CreatePartitionResponse(string path)
    {

        var owner = ResolveOwner(path);
        var part = path.Contains("part1", StringComparison.Ordinal)
            ? 1
            : path.Contains("part2", StringComparison.Ordinal) ? 2 : 3;
        var credits = string.Equals(owner, "OrgA", StringComparison.Ordinal)
            ? part * 10
            : part * 100;
        var json = $$"""
            {"day":"2026-09-01","user_id":{{credits}},"user_login":"user-{{credits}}","organization_id":"{{owner}}","ai_credits_used":{{credits}}}
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/octet-stream")
        };
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string ResolveOwner(string path)
    {

        if (path.Contains("/OrgA/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/orga-", StringComparison.OrdinalIgnoreCase))
            return "OrgA";

        if (path.Contains("/OrgB/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/orgb-", StringComparison.OrdinalIgnoreCase))
            return "OrgB";

        throw new InvalidOperationException($"No billing owner was encoded in '{path}'.");
    }

    private sealed class RoutingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
