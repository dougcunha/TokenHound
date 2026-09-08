using AwesomeAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Verifies that Antigravity preserves the quota group reported by its language server.
/// </summary>
public sealed class AntigravityUsageProviderModelGroupTests
{
    private const string QUOTA_RESPONSE = """
        {"response":{"groups":[{"displayName":"Gemini models","buckets":[{"bucketId":"gemini-weekly","displayName":"Weekly Limit","remainingFraction":0.80,"window":"weekly"}]},{"displayName":"Anthropic models","buckets":[{"bucketId":"anthropic-session","displayName":"Current Limit","remainingFraction":0.60,"window":"5h"}]}]}}
        """;

    /// <summary>
    /// Verifies that separate model groups retain their reported names.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenQuotaGroupsDiffer_PreservesGroupNames()
    {

        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: static () => [(1234, "--csrf_token token-abc")],
            portResolver: static _ => [5555]);
        using var httpClient = new HttpClient(new QuotaResponseHandler());
        using var client = new AntigravityLanguageServerClient(httpClient);
        using var provider = new AntigravityUsageProvider(discovery, client);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows.Select(static window => window.GroupName).Should().Equal("Gemini models", "Anthropic models");
    }

    private sealed class QuotaResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(QUOTA_RESPONSE, Encoding.UTF8, "application/json")
            });
    }
}
