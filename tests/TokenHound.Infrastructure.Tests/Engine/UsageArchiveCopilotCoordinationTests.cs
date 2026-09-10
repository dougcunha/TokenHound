using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies persistence coordination between the generic archive and extracted Copilot stores.
/// </summary>
public sealed class UsageArchiveCopilotCoordinationTests
{
    /// <summary>
    /// Verifies generic snapshots and Copilot HTTP state cannot overwrite each other during concurrent writes.
    /// </summary>
    [Fact]
    public async Task ConcurrentGenericAndCopilotHttpWrites_PreserveBothSections()
    {

        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(15);

            await WriteConcurrentStateAsync(archive, deadline);

            archive.Load().LastReadings.Should().HaveCount(20);
            archive.LoadCopilotHttpDeadline().Should().Be(new CopilotHttpGateState
            {
                DeadlineUtc = deadline,
                ConsecutiveFailures = 3
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static Task WriteConcurrentStateAsync(
        UsageArchive archive,
        DateTimeOffset deadline)
    {

        var snapshotWrites = Enumerable.Range(1, 20).Select(index => archive.SaveSnapshotAsync(
            CreateSnapshot($"provider-{index}"),
            TestContext.Current.CancellationToken
        ));
        var httpWrites = Enumerable.Range(1, 20).Select(_ => archive.SaveCopilotHttpDeadlineAsync(
            deadline,
            3,
            TestContext.Current.CancellationToken
        ));

        return Task.WhenAll(snapshotWrites.Concat(httpWrites));
    }

    /// <summary>
    /// Verifies extracted Copilot billing data is restored by a new archive instance.
    /// </summary>
    [Fact]
    public async Task Restart_RestoresCopilotBillingUsage()
    {

        var directory = CreateDirectory();
        var usage = CreateUsage();

        try
        {
            using (var archive = new UsageArchive(directory))
                await archive.SaveCopilotBillingAsync(usage, TestContext.Current.CancellationToken);

            using var restarted = new UsageArchive(directory);
            var restored = restarted.LoadCopilotBilling(
                usage.Context,
                usage.Period,
                usage.Coverage.Filters
            );

            restored.Should().NotBeNull();
            restored!.GrossUsed.Should().Be(42m);
            restored.Context.OwnerId.Should().Be("org-restart");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {

        var directory = Path.Combine(Path.GetTempPath(), $"archive-coordination-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        return directory;
    }

    private static Snapshot CreateSnapshot(string providerId)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = []
        };

    private static CopilotCreditUsage CreateUsage()
        => new()
        {
            Context = new CopilotBillingContext
            {
                PrincipalId = "restart-user",
                Scope = CopilotBillingScope.Organization,
                OwnerId = "org-restart",
                Plan = CopilotPlanType.Business
            },
            Period = new CopilotBillingPeriod
            {
                RequestedYear = 2026,
                RequestedMonth = 9
            },
            Coverage = new CopilotReportCoverage(),
            GrossUsed = 42m,
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = DateTimeOffset.UtcNow
        };
}
