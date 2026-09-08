using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies route construction, header formatting, and serialization for CopilotBillingClient.
/// </summary>
public sealed partial class CopilotBillingClientTests
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
          "grossQuantity": 12.345,
          "discountQuantity": 2.1,
          "netQuantity": 10.245,
          "grossAmount": 1.23,
          "discountAmount": 0.21,
          "netAmount": 1.02,
          "pricePerUnit": 0.10
        }
      ]
    }
    """;

    private const string SEATS_JSON = """
    {
      "total_seats": 3,
      "seats": [
        {
          "assignee": { "login": "testuser", "id": 12345 },
          "plan_type": "business",
          "created_at": "2026-01-01T00:00:00Z",
          "pending_cancellation_date": null
        },
        {
          "assignee": { "login": "otheruser", "id": 67890 },
          "plan_type": "business",
          "created_at": "2026-02-01T00:00:00Z",
          "pending_cancellation_date": null
        }
      ]
    }
    """;

    [Fact]
    public async Task GetBillingUsageAsync_SendsExactHeadersAndRouteForOrganization()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BILLING_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        var response = await client.GetBillingUsageAsync(
            CopilotBillingScope.Organization,
            "ColibriAgile",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://api.github.com/organizations/ColibriAgile/settings/billing/ai_credit/usage?year=2026&month=9", captured.RequestUri?.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization?.Scheme);
        Assert.Equal("fixture-token", captured.Headers.Authorization?.Parameter);
        Assert.Equal("application/vnd.github+json", captured.Headers.Accept.ToString());
        Assert.Equal("2026-03-10", captured.Headers.GetValues("X-GitHub-Api-Version").FirstOrDefault());
        Assert.Equal("TokenHound/1.0", captured.Headers.UserAgent.ToString());

        Assert.NotNull(response.TimePeriod);
        Assert.Equal(2026, response.TimePeriod.Year);
        Assert.Equal(9, response.TimePeriod.Month);
        Assert.Single(response.UsageItems!);

        var item = response.UsageItems![0];
        Assert.Equal("Copilot", item.Product);
        Assert.Equal("Copilot AI Credits", item.Sku);
        Assert.Equal("ai-credits", item.UnitType);
        Assert.Equal(12.345m, item.GrossQuantity);
        Assert.Equal(2.1m, item.DiscountQuantity);
        Assert.Equal(10.245m, item.NetQuantity);
    }

    [Fact]
    public async Task GetBillingUsageAsync_SendsExactRouteForPersonal()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BILLING_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        await client.GetBillingUsageAsync(
            CopilotBillingScope.Personal,
            "alice",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("https://api.github.com/users/alice/settings/billing/ai_credit/usage?year=2026&month=9", captured?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetBillingUsageAsync_SendsExactRouteForEnterprise()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BILLING_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        await client.GetBillingUsageAsync(
            CopilotBillingScope.Enterprise,
            "my-corp",
            2026,
            9,
            "fixture-token",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("https://api.github.com/enterprises/my-corp/settings/billing/ai_credit/usage?year=2026&month=9", captured?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetSeatAssignmentsAsync_SendsExactRouteAndDeserializesSeats()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SEATS_JSON, Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        var seatsResponse = await client.GetSeatAssignmentsAsync(
            "ColibriAgile",
            "fixture-token",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("https://api.github.com/orgs/ColibriAgile/copilot/billing/seats", captured?.RequestUri?.ToString());
        Assert.Equal(3, seatsResponse.TotalSeats);
        Assert.NotNull(seatsResponse.Seats);
        Assert.Equal(2, seatsResponse.Seats.Count);
        Assert.Equal("testuser", seatsResponse.Seats[0].Assignee?.Login);
        Assert.Equal("business", seatsResponse.Seats[0].PlanType);
    }

    [Fact]
    public async Task GetAuthenticatedUserLoginAsync_SendsExactRouteAndReturnsLogin()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"login\":\"octocat\",\"id\":1}", Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);

        var login = await client.GetAuthenticatedUserLoginAsync("fixture-token", TestContext.Current.CancellationToken);

        Assert.Equal("https://api.github.com/user", captured?.RequestUri?.ToString());
        Assert.Equal("octocat", login);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _asyncHandler;

        public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _asyncHandler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_asyncHandler is not null)
                return _asyncHandler(request, cancellationToken);

            return Task.FromResult(_handler!(request));
        }
    }
}
