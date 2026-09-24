using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Claude;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies quota normalization through Claude's existing HTTP and profile path.
/// </summary>
public sealed class ClaudeQuotaProviderTests
{
    private const string CREDENTIAL = """{"claudeAiOauth":{"accessToken":"sk-valid","expiresAt":253402300799000}}""";
    private const string QUOTAS = """
        {
          "limits": [
            {"kind":"session","percent":35,"resets_at":"2026-09-23T18:00:00Z"},
            {"kind":"weekly_all","percent":72},
            {"kind":"weekly_scoped","group":"weekly","percent":0,
             "scope":{"model":{"id":null,"display_name":"Fable"},"surface":null}},
            {"kind":"weekly_opus","percent":24}
          ],
          "five_hour":{"utilization":99},
          "seven_day":{"utilization":99},
          "seven_day_opus":{"utilization":99},
          "seven_day_sonnet":{"utilization":0},
          "seven_day_haiku":null,
          "seven_day_breakdown":{"models":[]},
          "extra_usage":{"utilization":95}
        }
        """;

    /// <summary>
    /// Verifies base aliases, named quotas, scoped quotas, and percent-only values.
    /// </summary>
    [Fact]
    public async Task ReportedQuotas_AreOrderedAndKeepOnlyReportedMeasurements()
    {

        using var scope = new ProfileScope();
        using var http = new HttpClient(new StubHandler(static _ => Ok(QUOTAS)));
        var provider = CreateProvider(scope, http);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.LimitWindows.Select(static window => window.Name).Should().Equal(
            "five_hour", "seven_day", "seven_day_opus", "seven_day_sonnet", "weekly_scoped");
        snapshot.LimitWindows[0].UsedFraction.Should().Be(0.35);
        snapshot.LimitWindows[1].UsedFraction.Should().Be(0.72);
        snapshot.LimitWindows[2].UsedFraction.Should().Be(0.24);
        snapshot.LimitWindows[3].UsedFraction.Should().Be(0);
        snapshot.LimitWindows[4].GroupName.Should().Be("Fable");
        snapshot.LimitWindows.All(static window => window.TotalUnits is null && window.RemainingUnits is null)
            .Should().BeTrue();
    }

    /// <summary>
    /// Verifies that malformed entries cannot remove valid neighbors or the rollover fallback.
    /// </summary>
    [Fact]
    public async Task MalformedEntries_AreSkippedIndividuallyAtRollover()
    {

        const string json = """
            {
              "limits":[null,7,{"kind":"weekly_bad","percent":"oops"},
                {"kind":"weekly_high","percent":101},
                {"kind":"weekly_scoped","percent":0,"scope":"Work","resets_at":"invalid"}],
              "five_hour":{"utilization":0,"resets_at":"invalid"},
              "seven_day":{"utilization":32},
              "seven_day_opus":{"utilization":"bad"},
              "seven_day_sonnet":{"utilization":null}
            }
            """;
        using var scope = new ProfileScope();
        using var http = new HttpClient(new StubHandler(static _ => Ok(json)));

        var snapshot = await CreateProvider(scope, http).GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows.Select(static window => window.Name).Should().Equal(
            "five_hour", "seven_day", "weekly_scoped");
        snapshot.LimitWindows[0].UsedFraction.Should().Be(0);
        snapshot.LimitWindows[0].ResetTimeUtc.Should().BeNull();
        snapshot.LimitWindows[2].GroupName.Should().Be("Work");
        snapshot.LimitWindows[2].ResetTimeUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that model and surface scopes form separate identities and the category group is never a scope.
    /// </summary>
    [Fact]
    public async Task ScopedGroups_AreNotDeduplicatedByKindOrPercentage()
    {

        const string json = """
            {"limits":[{"kind":"weekly_scoped","group":"weekly","percent":12,"scope":{"model":{"display_name":"Opus"}}},
                       {"kind":"weekly_scoped","group":"weekly","percent":12,"scope":{"surface":"Cowork"}},
                       {"kind":"weekly_scoped","group":"weekly","percent":12,"scope":{"model":null,"surface":null}}]}
            """;
        using var scope = new ProfileScope();
        using var http = new HttpClient(new StubHandler(static _ => Ok(json)));

        var snapshot = await CreateProvider(scope, http).GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.LimitWindows.Should().HaveCount(3);
        snapshot.LimitWindows.Select(static window => window.GroupName).Should().Equal(null, "Cowork", "Opus");
    }

    /// <summary>
    /// Verifies that profile-specific responses do not share optional quotas.
    /// </summary>
    [Fact]
    public async Task SeparateProfiles_KeepTheirOwnQuotaBreakdowns()
    {

        using var firstScope = new ProfileScope();
        using var secondScope = new ProfileScope();
        using var firstHttp = new HttpClient(new StubHandler(static _ => Ok(QUOTAS)));
        using var secondHttp = new HttpClient(new StubHandler(static _ => Ok("""{"five_hour":{"utilization":8}}""")));

        var first = await CreateProvider(firstScope, firstHttp).GetSnapshotAsync(TestContext.Current.CancellationToken);
        var second = await CreateProvider(secondScope, secondHttp).GetSnapshotAsync(TestContext.Current.CancellationToken);

        first.LimitWindows.Should().HaveCount(5);
        second.LimitWindows.Should().ContainSingle().Which.UsedFraction.Should().Be(0.08);
    }

    /// <summary>
    /// Verifies full stale carry-forward and a future deadline after Retry-After zero.
    /// </summary>
    [Fact]
    public async Task RateLimitAfterSuccess_KeepsTheWholeBreakdown()
    {

        using var scope = new ProfileScope();
        var requests = 0;
        using var http = new HttpClient(new StubHandler(_ => ++requests == 1 ? Ok(QUOTAS) : RateLimited()));
        var provider = CreateProvider(scope, http);

        var successful = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);
        var blocked = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        blocked.Status.Should().Be(ProviderStatus.RateLimited);
        blocked.LimitWindows.Should().Equal(successful.LimitWindows);
        blocked.ActiveBlock!.ResetTimeUtc.Should().BeAfter(blocked.FetchedAtUtc);
        requests.Should().Be(2);
    }

    private static ClaudeOAuthProvider CreateProvider(ProfileScope scope, HttpClient http)
        => new(new ClaudeProfileDiscovery(scope.DirectoryPath), new ClaudeOAuthClient(http));

    private static HttpResponseMessage Ok(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage RateLimited()
    {

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "0");

        return response;
    }

    private sealed class ProfileScope : IDisposable
    {
        public ProfileScope()
        {

            DirectoryPath = Path.Combine(Path.GetTempPath(), $"claude_quotas_{Guid.NewGuid():N}");
            var profile = Path.Combine(DirectoryPath, ".claude");
            Directory.CreateDirectory(profile);
            File.WriteAllText(Path.Combine(profile, ".credentials.json"), CREDENTIAL);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {

            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
