using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies manifest dispatch, credential-free downloads, security validations, and redirect handling for CopilotMetricsClient.
/// </summary>
public sealed class CopilotMetricsClientTests
{
    private const string MANIFEST_JSON = """
    {
      "report_day": "2026-09-06",
      "download_links": [
        "https://reports.github.com/data/part1.ndjson",
        "https://reports.github.com/data/part2.ndjson"
      ]
    }
    """;

    [Fact]
    public async Task GetMetricsManifestAsync_SendsCorrectHeadersAndDeserializesResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHandler(req =>
        {
            capturedRequest = req;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MANIFEST_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);

        var manifest = await client.GetMetricsManifestAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            new DateOnly(2026, 9, 6),
            "test-token",
            null,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(manifest);
        Assert.Equal(new DateOnly(2026, 9, 6), manifest.ReportDay);
        Assert.Equal(2, manifest.DownloadLinks.Count);
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("TokenHound/1.0", capturedRequest.Headers.UserAgent.ToString());
        Assert.Contains(capturedRequest.Headers.Accept, static h => h.MediaType == "application/vnd.github+json");
    }

    [Fact]
    public async Task GetMetricsManifestAsync_WhenNotFoundOrNoContent_ReturnsNull()
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);

        var manifest = await client.GetMetricsManifestAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            new DateOnly(2026, 9, 6),
            "test-token",
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Null(manifest);
    }

    [Fact]
    public async Task DownloadReportAsync_SendsNoAuthorizationHeaderAndNoCookies()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHandler(req =>
        {
            capturedRequest = req;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/octet-stream")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);

        using var stream = await client.DownloadReportAsync(
            "https://reports.github.com/data/part1.ndjson",
            null,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(stream);
        Assert.NotNull(capturedRequest);
        Assert.Null(capturedRequest.Headers.Authorization);
        Assert.False(capturedRequest.Headers.Contains("Cookie"));
    }

    [Theory]
    [InlineData("http://reports.github.com/data/part1.ndjson")]
    [InlineData("https://localhost/data/part1.ndjson")]
    [InlineData("https://127.0.0.1/data/part1.ndjson")]
    [InlineData("https://192.168.1.10/data/part1.ndjson")]
    [InlineData("https://10.0.0.5/data/part1.ndjson")]
    [InlineData("https://169.254.169.254/data/part1.ndjson")]
    [InlineData("https://internal.local/data/part1.ndjson")]
    public async Task DownloadReportAsync_WhenDestinationUnsafe_ThrowsSecurityException(string unsafeUrl)
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);

        await Assert.ThrowsAsync<SecurityException>(() => client.DownloadReportAsync(
            unsafeUrl,
            null,
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task DownloadReportAsync_WhenUnsafeRedirect_ThrowsSecurityException()
    {
        var handler = new TestHandler(req =>
        {
            var redirectResp = new HttpResponseMessage(HttpStatusCode.Redirect);
            redirectResp.Headers.Location = new Uri("http://unsafe.github.com/plain-http");

            return redirectResp;
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);

        await Assert.ThrowsAsync<SecurityException>(() => client.DownloadReportAsync(
            "https://reports.github.com/redirect",
            null,
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task DownloadReportAsync_WhenSafeRedirect_FollowsAndConsumesBudget()
    {
        var requestCount = 0;
        var handler = new TestHandler(req =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                var redirectResp = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirectResp.Headers.Location = new Uri("https://cdn.reports.github.com/final.ndjson");

                return redirectResp;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"day\":\"2026-09-06\"}", Encoding.UTF8, "application/octet-stream")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http);
        var budget = new CopilotPassDispatchBudget(4);

        using var stream = await client.DownloadReportAsync(
            "https://reports.github.com/redirect",
            budget,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(stream);
        Assert.Equal(2, requestCount);
        Assert.Equal(2, budget.DispatchesUsed);
    }

    [Fact]
    public async Task DownloadReportAsync_WhenRateLimited_RecordsDeadlineOnGate()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"metrics_client_test_{Guid.NewGuid():N}");
        try
        {
            var handler = new TestHandler(_ =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                resp.Headers.TryAddWithoutValidation("Retry-After", "120");

                return resp;
            });

            using var http = new HttpClient(handler);
            var archive = new UsageArchive(tempDir);
            var gate = new CopilotRequestGate(archive);
            using var client = new CopilotMetricsClient(manifestHttpClient: http, downloadHttpClient: http, gate: gate);

            await Assert.ThrowsAsync<CopilotApiException>(() => client.DownloadReportAsync(
                "https://reports.github.com/data.ndjson",
                null,
                TestContext.Current.CancellationToken
            ));

            Assert.False(gate.CanDispatch);
            Assert.NotNull(gate.ActiveDeadlineUtc);
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
