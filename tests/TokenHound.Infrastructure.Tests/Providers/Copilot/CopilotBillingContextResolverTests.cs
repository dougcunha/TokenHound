using System;
using System.Collections.Generic;
using System.IO;
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
/// Verifies principal resolution, candidate seat verification, and plan normalization for CopilotBillingContextResolver.
/// </summary>
public sealed partial class CopilotBillingContextResolverTests
{
    private const string MATCHING_SEATS_JSON = """
    {
      "total_seats": 1,
      "seats": [
        {
          "assignee": { "login": "alice", "id": 100 },
          "plan_type": "business",
          "created_at": "2026-01-01T00:00:00Z",
          "pending_cancellation_date": null
        }
      ]
    }
    """;

    private const string NON_MATCHING_SEATS_JSON = """
    {
      "total_seats": 1,
      "seats": [
        {
          "assignee": { "login": "bob", "id": 200 },
          "plan_type": "business",
          "created_at": "2026-01-01T00:00:00Z",
          "pending_cancellation_date": null
        }
      ]
    }
    """;

    [Fact]
    public async Task ResolveContextAsync_WhenCandidateOrgHasMatchingSeatAssignment_ResolvesOrganizationScope()
    {
        var handler = new TestHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(MATCHING_SEATS_JSON, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = "alice",
            CopilotPlan = "business",
            OrganizationLoginList = ["ColibriAgile"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal("alice", context.PrincipalId);
        Assert.Equal(CopilotBillingScope.Organization, context.Scope);
        Assert.Equal("ColibriAgile", context.OwnerId);
        Assert.Equal("ColibriAgile", context.OwnerName);
        Assert.Equal(CopilotPlanType.Business, context.Plan);
        Assert.Equal("seats:ColibriAgile", context.EvidenceKey);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenLoginMissingInQuota_ResolvesPrincipalFromUserEndpoint()
    {
        var handler = new TestHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.EndsWith("/user", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"login\":\"alice\",\"id\":100}", Encoding.UTF8, "application/json")
                };
            }

            if (req.RequestUri?.AbsolutePath.Contains("/seats", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(MATCHING_SEATS_JSON, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = null,
            CopilotPlan = "business",
            OrganizationLoginList = ["ColibriAgile"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal("alice", context.PrincipalId);
        Assert.Equal(CopilotBillingScope.Organization, context.Scope);
        Assert.Equal("ColibriAgile", context.OwnerId);
    }

    [Theory]
    [InlineData("business", CopilotPlanType.Business)]
    [InlineData("enterprise", CopilotPlanType.Enterprise)]
    [InlineData("pro", CopilotPlanType.Pro)]
    [InlineData("pro_plus", CopilotPlanType.ProPlus)]
    [InlineData("free", CopilotPlanType.Free)]
    [InlineData("max", CopilotPlanType.Max)]
    public async Task ResolveContextAsync_NormalizesPlanTypes(string rawPlan, CopilotPlanType expectedPlan)
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(MATCHING_SEATS_JSON, Encoding.UTF8, "application/json")
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = "alice",
            CopilotPlan = rawPlan,
            OrganizationLoginList = ["ColibriAgile"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal(expectedPlan, context.Plan);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenWorkspaceYamlCorroboratesOrg_IncludesAsCandidate()
    {
        var tempSessionDir = Path.Combine(Path.GetTempPath(), $"copilot_test_session_{Guid.NewGuid():N}");
        var sessionSubdir = Path.Combine(tempSessionDir, "session-123");
        Directory.CreateDirectory(sessionSubdir);

        try
        {
            var yamlPath = Path.Combine(sessionSubdir, "workspace.yaml");
            File.WriteAllText(yamlPath, "repository: ColibriAgile/MyProject\nbranch: main\n");

            var handler = new TestHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("/ColibriAgile/copilot/billing/seats", StringComparison.Ordinal) == true)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(MATCHING_SEATS_JSON, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            using var http = new HttpClient(handler);
            using var client = new CopilotBillingClient(http);
            var resolver = new CopilotBillingContextResolver(client, tempSessionDir);

            var quota = new CopilotQuotaResponse
            {
                Login = "alice",
                CopilotPlan = "business",
                OrganizationLoginList = []
            };

            var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

            Assert.Equal(CopilotBillingScope.Organization, context.Scope);
            Assert.Equal("ColibriAgile", context.OwnerId);
        }
        finally
        {
            if (Directory.Exists(tempSessionDir))
                Directory.Delete(tempSessionDir, true);
        }
    }

    [Fact]
    public async Task ResolveContextAsync_WhenSeatAssignmentDoesNotMatchPrincipal_ReturnsUnknownScope()
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(NON_MATCHING_SEATS_JSON, Encoding.UTF8, "application/json")
        });

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = "alice",
            CopilotPlan = "business",
            OrganizationLoginList = ["ColibriAgile"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal(CopilotBillingScope.Unknown, context.Scope);
        Assert.Null(context.OwnerId);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenSeatAssignmentReturns403Or404_ReturnsUnknownScope()
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = "alice",
            CopilotPlan = "business",
            OrganizationLoginList = ["ColibriAgile"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal(CopilotBillingScope.Unknown, context.Scope);
    }
}
