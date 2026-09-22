using System;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>Checks source-specific invariants against the installed Antigravity environment.</summary>
public sealed class AntigravityLiveUsageProviderTests
{
    /// <summary>Verifies successful official and transcript readings retain their distinct meanings.</summary>
    [Fact]
    public async Task GetSnapshotAsync_LiveIntegration_ReportsSourceAppropriateMetrics()
    {
        using var provider = new AntigravityUsageProvider();
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal("gemini", snapshot.ProviderId);
        Assert.NotEqual(default(DateTimeOffset), snapshot.FetchedAtUtc);

        if (snapshot.Status != ProviderStatus.Ok)
            return;

        Assert.NotEmpty(snapshot.LimitWindows);

        if (snapshot.Fidelity == Fidelity.Derived)
        {
            var window = Assert.Single(snapshot.LimitWindows);
            Assert.Equal("Requests Today", window.Name);
            Assert.NotNull(window.UsedUnits);
            Assert.Null(window.RemainingUnits);
            Assert.Null(window.UsedFraction);
            Assert.Null(window.TotalUnits);

            return;
        }

        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
    }
}
