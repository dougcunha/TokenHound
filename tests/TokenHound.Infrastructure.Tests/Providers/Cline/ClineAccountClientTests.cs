using AwesomeAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies the Cline account client header contract, payload mapping, and failure classification.
/// </summary>
public sealed class ClineAccountClientTests
{
    private const string TEST_TOKEN = "workos:test-jwt";

    /// <summary>Verifies that the Bearer header forwards the stored token verbatim, prefix included.</summary>
    [Fact]
    public async Task GetUserAsync_ForwardsStoredTokenVerbatim()
    {

        const string payload = """{"success":true,"data":{"id":"usr-01TEST"}}""";
        var handler = new CaptureHandler(static _ => JsonResponse(payload));

        using var client = new ClineAccountClient(handler);

        var user = await client.GetUserAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
        user!.Id.Should().Be("usr-01TEST");
        handler.LastAuthorization.Should().Be($"Bearer {TEST_TOKEN}");
        handler.LastUserAgent.Should().Be("TokenHound");
    }

    /// <summary>Verifies that balance and plan payloads deserialize with their published field names.</summary>
    [Fact]
    public async Task GetBalanceAndPlanAsync_DeserializePublishedFields()
    {

        var handler = new CaptureHandler(static request => request.RequestUri!.AbsolutePath.EndsWith("/balance")
            ? JsonResponse("""{"success":true,"data":{"userId":"usr-01TEST","balance":12.5}}""")
            : JsonResponse("""{"success":true,"data":{"plan":{"displayName":"Cline Pass (Monthly)","entitlements":{"cline_pass":{"enabled":true,"inferenceCapThreshold":{"last5HoursUsageCostUSDPerUser":1000,"last7daysUsageCostUSDPerUser":2500,"last30daysUsageCostUSDPerUser":5000}}}}}}"""));

        using var client = new ClineAccountClient(handler);

        var balance = await client.GetBalanceAsync(TEST_TOKEN, "usr-01TEST", TestContext.Current.CancellationToken);
        var plan = await client.GetPlanAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        balance.Should().NotBeNull();
        balance!.Balance.Should().Be(12.5);
        plan.Should().NotBeNull();
        plan!.Plan!.DisplayName.Should().Be("Cline Pass (Monthly)");
        plan.Plan.Entitlements!.ClinePass!.InferenceCapThreshold!.Last5HoursCost.Should().Be(1000);
    }

    /// <summary>Verifies that a 404 plan response maps to a missing plan instead of a failure.</summary>
    [Fact]
    public async Task GetPlanAsync_WhenNotFound_ReturnsNull()
    {

        var handler = new CaptureHandler(static _ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"data":null,"error":"no plan history found for user","success":false}""",
                    Encoding.UTF8,
                    "application/json")
            });

        using var client = new ClineAccountClient(handler);

        var plan = await client.GetPlanAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        plan.Should().BeNull();
    }

    /// <summary>Verifies that HTTP 401 surfaces the account authentication exception.</summary>
    [Fact]
    public async Task GetUserAsync_WhenUnauthorized_ThrowsClineAuthException()
    {

        var handler = new CaptureHandler(static _ => StatusResponse(HttpStatusCode.Unauthorized));

        using var client = new ClineAccountClient(handler);

        var act = async () => await client.GetUserAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClineAuthException>();
    }

    /// <summary>Verifies that HTTP 403 surfaces the entitlement exception.</summary>
    [Fact]
    public async Task GetUserAsync_WhenForbidden_ThrowsClineEntitlementException()
    {

        var handler = new CaptureHandler(static _ => StatusResponse(HttpStatusCode.Forbidden));

        using var client = new ClineAccountClient(handler);

        var act = async () => await client.GetUserAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ClineEntitlementException>();
    }

    /// <summary>Verifies that HTTP 429 surfaces the rate-limit exception with the Retry-After value.</summary>
    [Fact]
    public async Task GetUserAsync_WhenRateLimited_ThrowsWithRetryAfter()
    {

        var handler = new CaptureHandler(static _ =>
        {
            var response = StatusResponse(HttpStatusCode.TooManyRequests);
            response.Headers.Add("Retry-After", "45");
            return response;
        });

        using var client = new ClineAccountClient(handler);

        var act = async () => await client.GetUserAsync(TEST_TOKEN, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ClineRateLimitException>();
        exception.Which.RetryAfterSeconds.Should().Be(45);
    }

    /// <summary>Verifies that usage transactions deserialize token buckets and tiers.</summary>
    [Fact]
    public async Task GetUsageAsync_DeserializesTransactions()
    {

        const string payload = """
            {"success":true,"data":{"items":[{
              "createdAt":"2026-09-16T19:28:18.719887Z",
              "costUsd":2688570,
              "creditsUsed":0,
              "promptTokens":174710,
              "completionTokens":1132,
              "totalTokens":175842,
              "aiModelName":"Deepseek-v4.1-Flash",
              "aiModelTypeName":"cline-free"
            }]}}
            """;
        var handler = new CaptureHandler(static _ => JsonResponse(payload));

        using var client = new ClineAccountClient(handler);

        var items = await client.GetUsageAsync(TEST_TOKEN, "usr-01TEST", TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].TotalTokens.Should().Be(175842);
        items[0].ModelTypeName.Should().Be("cline-free");
        items[0].CreditsUsed.Should().Be(0);
    }

    private static HttpResponseMessage JsonResponse(string payload)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage StatusResponse(HttpStatusCode status)
        => new(status);

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public string? LastAuthorization { get; private set; }

        public string? LastUserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {

            LastAuthorization = request.Headers.TryGetValues("Authorization", out var auth)
                ? string.Join(",", auth)
                : null;
            LastUserAgent = request.Headers.UserAgent.ToString();

            return Task.FromResult(handler(request));
        }
    }
}