using System;
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

public sealed partial class CopilotBillingContextResolverTests
{
    [Fact]
    public async Task ResolveContextAsync_WhenMultipleCandidateOrgsMatch_ReturnsUnknownScopeWithAmbiguousKey()
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
            CopilotPlan = "business",
            OrganizationLoginList = ["OrgAlpha", "OrgBeta"]
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal(CopilotBillingScope.Unknown, context.Scope);
        Assert.Equal("ambiguous", context.EvidenceKey);
    }

    [Fact]
    public async Task ResolveContextAsync_WhenNoCandidatesAndNoWorkspace_ReturnsUnknownScope()
    {
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        using var http = new HttpClient(handler);
        using var client = new CopilotBillingClient(http);
        var resolver = new CopilotBillingContextResolver(client, Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid():N}"));

        var quota = new CopilotQuotaResponse
        {
            Login = "alice",
            CopilotPlan = "business",
            OrganizationLoginList = null
        };

        var context = await resolver.ResolveContextAsync("fixture-token", quota, TestContext.Current.CancellationToken);

        Assert.Equal(CopilotBillingScope.Unknown, context.Scope);
        Assert.Null(context.OwnerId);
    }

    private sealed class TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
