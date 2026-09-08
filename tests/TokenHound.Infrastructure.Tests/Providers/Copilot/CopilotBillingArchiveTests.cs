using AwesomeAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies independent billing archive persistence, key isolation, period pruning, and corruption safety.
/// </summary>
public sealed class CopilotBillingArchiveTests
{
    /// <summary>Verifies that Copilot credit usage round-trips through copilot_billing.json.</summary>
    [Fact]
    public async Task SaveAndLoad_RoundTripsBillingUsage()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var usage = CreateUsage("user-1", CopilotBillingScope.Organization, "org-a", 2026, 9, 1500m);

            await archive.SaveCopilotBillingAsync(usage, TestContext.Current.CancellationToken);

            var loaded = archive.LoadCopilotBilling(usage.Context, usage.Period, usage.Coverage.Filters);

            loaded.Should().NotBeNull();
            loaded!.GrossUsed.Should().Be(1500m);
            loaded.Context.PrincipalId.Should().Be("user-1");
            loaded.Context.OwnerId.Should().Be("org-a");
            loaded.Period.RequestedYear.Should().Be(2026);
            loaded.Period.RequestedMonth.Should().Be(9);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that daily usage summaries round-trip and update atomically.</summary>
    [Fact]
    public async Task SaveAndLoad_RoundTripsDailySummaries()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var context = CreateContext("user-1", CopilotBillingScope.Organization, "org-a");
            var period = CreatePeriod(2026, 9);
            var day1 = new CopilotDailyUsageSummary
            {
                Day = new DateOnly(2026, 9, 1),
                AiCreditsUsed = 100m,
                UserCount = 5,
                FetchedAtUtc = DateTimeOffset.UtcNow
            };
            var day2 = new CopilotDailyUsageSummary
            {
                Day = new DateOnly(2026, 9, 2),
                AiCreditsUsed = 250m,
                UserCount = 8,
                FetchedAtUtc = DateTimeOffset.UtcNow
            };

            await archive.SaveCopilotDailySummariesAsync(
                context,
                period,
                [day1, day2],
                null,
                TestContext.Current.CancellationToken
            );

            var summaries = archive.LoadCopilotDailySummaries(context, period);

            summaries.Should().HaveCount(2);
            summaries[0].Day.Should().Be(new DateOnly(2026, 9, 1));
            summaries[0].AiCreditsUsed.Should().Be(100m);
            summaries[1].Day.Should().Be(new DateOnly(2026, 9, 2));
            summaries[1].AiCreditsUsed.Should().Be(250m);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that cache entries cannot cross principal, owner, period, or filter boundaries.</summary>
    [Fact]
    public async Task LoadCopilotBilling_EnforcesStrictKeyIsolation()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var usage = CreateUsage("user-1", CopilotBillingScope.Organization, "org-a", 2026, 9, 500m);

            await archive.SaveCopilotBillingAsync(usage, TestContext.Current.CancellationToken);

            // Different principal
            archive.LoadCopilotBilling("user-2", CopilotBillingScope.Organization, "org-a", 2026, 9).Should().BeNull();

            // Different scope
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Personal, "org-a", 2026, 9).Should().BeNull();

            // Different owner
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Organization, "org-b", 2026, 9).Should().BeNull();

            // Different period
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Organization, "org-a", 2026, 8).Should().BeNull();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that saving a new period prunes entries older than the immediately preceding period.</summary>
    [Fact]
    public async Task SaveCopilotBilling_PrunesEntriesOlderThanPrecedingPeriod()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);

            // Save July (month 7), August (month 8), September (month 9)
            var july = CreateUsage("user-1", CopilotBillingScope.Organization, "org-a", 2026, 7, 700m);
            var august = CreateUsage("user-1", CopilotBillingScope.Organization, "org-a", 2026, 8, 800m);
            var september = CreateUsage("user-1", CopilotBillingScope.Organization, "org-a", 2026, 9, 900m);

            await archive.SaveCopilotBillingAsync(july, TestContext.Current.CancellationToken);
            await archive.SaveCopilotBillingAsync(august, TestContext.Current.CancellationToken);
            await archive.SaveCopilotBillingAsync(september, TestContext.Current.CancellationToken);

            // July should be pruned (older than preceding August)
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Organization, "org-a", 2026, 7).Should().BeNull();

            // August (preceding) and September (current) must survive
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Organization, "org-a", 2026, 8).Should().NotBeNull();
            archive.LoadCopilotBilling("user-1", CopilotBillingScope.Organization, "org-a", 2026, 9).Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that corrupt copilot_billing.json does not erase or corrupt quota or deadline files.</summary>
    [Fact]
    public async Task CorruptBillingJson_DoesNotEraseQuotaOrDeadlineFiles()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var snapshot = new Snapshot
            {
                ProviderId = "copilot",
                Status = ProviderStatus.Ok,
                Fidelity = Fidelity.Official,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                LimitWindows = []
            };
            var deadline = DateTimeOffset.UtcNow.AddHours(1);

            await archive.SaveSnapshotAsync(snapshot, TestContext.Current.CancellationToken);
            await archive.SaveBackoffDeadlineAsync("copilot", deadline, TestContext.Current.CancellationToken);

            // Write malformed JSON to copilot_billing.json
            await File.WriteAllTextAsync(archive.CopilotBillingPath, "{ not valid json !!", TestContext.Current.CancellationToken);

            var loadedBilling = archive.LoadCopilotBilling("user-1", CopilotBillingScope.Personal, null, 2026, 9);
            loadedBilling.Should().BeNull();

            // Quota and deadline files must remain intact and valid
            var state = archive.Load();
            state.LastReadings.Should().ContainKey("copilot");
            state.BackoffDeadlines.Should().ContainKey("copilot");
            state.BackoffDeadlines["copilot"].Should().Be(deadline);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "TokenHound", $"billing-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static CopilotBillingContext CreateContext(string principalId, CopilotBillingScope scope, string? ownerId)
        => new()
        {
            PrincipalId = principalId,
            Scope = scope,
            OwnerId = ownerId,
            Plan = CopilotPlanType.Business
        };

    private static CopilotBillingPeriod CreatePeriod(int year, int month)
        => new()
        {
            RequestedYear = year,
            RequestedMonth = month
        };

    private static CopilotCreditUsage CreateUsage(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        decimal grossUsed)
        => new()
        {
            Context = CreateContext(principalId, scope, ownerId),
            Period = CreatePeriod(year, month),
            Coverage = new CopilotReportCoverage(),
            GrossUsed = grossUsed,
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = DateTimeOffset.UtcNow
        };
}
