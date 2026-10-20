using AwesomeAssertions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Verifies that Antigravity preserves the quota group reported by its language server and Cloud Code API.
/// </summary>
public sealed class AntigravityUsageProviderModelGroupTests
{
    private const string QUOTA_RESPONSE = """
        {"response":{"groups":[{"displayName":"Gemini models","buckets":[{"bucketId":"gemini-weekly","displayName":"Weekly Limit","remainingFraction":0.80,"window":"weekly"}]},{"displayName":"Anthropic models","buckets":[{"bucketId":"anthropic-session","displayName":"Current Limit","remainingFraction":0.60,"window":"5h"}]}]}}
        """;

    private const string PROTO_QUOTA_RESPONSE = """
        {"response":{"groups":[{"groupName":"Gemini Models","buckets":[{"quotaType":1,"remainingFraction":0.96,"resetTime":"2026-10-20T22:53:46Z"},{"quotaType":"QUOTA_TYPE_WEEKLY","remainingFraction":0.72,"resetTime":"2026-10-25T14:43:39Z"}]}]}}
        """;

    /// <summary>
    /// Verifies that separate model groups retain their reported names.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenQuotaGroupsDiffer_PreservesGroupNames()
    {
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: static () => [(1234, "--csrf_token token-abc")],
            portResolver: static _ => [5555]
        );
        using var httpClient = new HttpClient(new QuotaResponseHandler(QUOTA_RESPONSE));
        using var client = new AntigravityLanguageServerClient(httpClient);
        using var provider = new AntigravityUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows.Select(static window => window.GroupName).Should().Equal("Gemini models", "Anthropic models");
    }

    /// <summary>
    /// Verifies that protobuf groupName and quotaType values map to group names, window names, and periods.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenProtoGroupNameAndQuotaTypeReturned_MapsGroupNameAndPeriod()
    {
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: static () => [(1234, "--csrf_token token-abc")],
            portResolver: static _ => [5555]
        );
        using var httpClient = new HttpClient(new QuotaResponseHandler(PROTO_QUOTA_RESPONSE));
        using var client = new AntigravityLanguageServerClient(httpClient);
        using var provider = new AntigravityUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows.Should().HaveCount(2);
        snapshot.LimitWindows[0].Name.Should().Be("5h Limit");
        snapshot.LimitWindows[0].GroupName.Should().Be("Gemini Models");
        snapshot.LimitWindows[0].Period.Should().Be(TimeSpan.FromHours(5));
        snapshot.LimitWindows[1].Name.Should().Be("Weekly Limit");
        snapshot.LimitWindows[1].GroupName.Should().Be("Gemini Models");
        snapshot.LimitWindows[1].Period.Should().Be(TimeSpan.FromDays(7));
    }

    private sealed class QuotaResponseHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
    }
}
