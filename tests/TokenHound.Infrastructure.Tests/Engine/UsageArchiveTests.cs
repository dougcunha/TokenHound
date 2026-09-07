using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies TokenHound-owned snapshot and deadline archive behavior.
/// </summary>
public sealed class UsageArchiveTests
{
    /// <summary>
    /// Verifies that snapshots and deadlines round-trip through separate archive files.
    /// </summary>
    [Fact]
    public async Task SaveAndLoad_RoundTripsSnapshotsAndDeadlines()
    {
        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);
            var snapshot = CreateSnapshot("copilot", DateTimeOffset.UtcNow.AddMinutes(-2));
            var deadline = DateTimeOffset.UtcNow.AddMinutes(5);

            await archive.SaveSnapshotAsync(snapshot, TestContext.Current.CancellationToken);
            await archive.SaveBackoffDeadlineAsync(
                "copilot",
                deadline,
                TestContext.Current.CancellationToken
            );

            var state = archive.Load();

            state.ErrorDescription.Should().BeNull();
            state.LastReadings["copilot"].ProviderId.Should().Be(snapshot.ProviderId);
            state.LastReadings["copilot"].Status.Should().Be(snapshot.Status);
            state.LastReadings["copilot"].FetchedAtUtc.Should().Be(snapshot.FetchedAtUtc);
            state.LastReadings["copilot"].LimitWindows.Should().BeEmpty();
            state.BackoffDeadlines["copilot"].Should().Be(deadline);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Verifies that unrelated state keys survive a provider deadline update.
    /// </summary>
    [Fact]
    public async Task SaveBackoffDeadline_PreservesUnrelatedStateKeys()
    {
        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(
                archive.StatePath,
                "{\"unrelated\":{\"value\":42},\"backoffUntil\":{\"claude\":\"2026-09-06T12:00:00.0000000+00:00\"}}",
                TestContext.Current.CancellationToken
            );

            await archive.SaveBackoffDeadlineAsync(
                "copilot",
                DateTimeOffset.UtcNow.AddMinutes(1),
                TestContext.Current.CancellationToken
            );

            using var document = JsonDocument.Parse(
                await File.ReadAllTextAsync(
                    archive.StatePath,
                    TestContext.Current.CancellationToken
                )
            );
            document.RootElement.GetProperty("unrelated").GetProperty("value").GetInt32().Should().Be(42);
            document.RootElement.GetProperty("backoffUntil").GetProperty("claude").GetString().Should().NotBeNull();
            document.RootElement.GetProperty("backoffUntil").GetProperty("copilot").GetString().Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Verifies that missing files produce an empty state without creating files.
    /// </summary>
    [Fact]
    public void Load_WhenFilesAreMissing_ReturnsEmptyStateWithoutError()
    {
        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);

            var state = archive.Load();

            state.ErrorDescription.Should().BeNull();
            state.LastReadings.Should().BeEmpty();
            state.BackoffDeadlines.Should().BeEmpty();
            File.Exists(archive.LastReadingsPath).Should().BeFalse();
            File.Exists(archive.StatePath).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Verifies that malformed state is reported and cannot fabricate a deadline.
    /// </summary>
    [Fact]
    public async Task Load_WhenStateIsMalformed_ReportsErrorAndReturnsNoDeadline()
    {
        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);
            await File.WriteAllTextAsync(archive.StatePath, "not-json", TestContext.Current.CancellationToken);

            var state = archive.Load();

            state.ErrorDescription.Should().Contain("state.json");
            state.BackoffDeadlines.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Verifies that concurrent writes leave a readable complete archive without temporary files.
    /// </summary>
    [Fact]
    public async Task ConcurrentWrites_AreSerializedAndAtomic()
    {
        var directory = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(directory);
            var writes = Enumerable.Range(1, 8)
                .Select(index => archive.SaveSnapshotAsync(
                    CreateSnapshot($"provider-{index}", DateTimeOffset.UtcNow),
                    TestContext.Current.CancellationToken));

            await Task.WhenAll(writes);

            var state = archive.Load();

            state.LastReadings.Should().HaveCount(8);
            Directory.GetFiles(directory, "*.tmp").Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {

        var directory = Path.Combine(
            Path.GetTempPath(),
            "TokenHound",
            $"archive-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        return directory;
    }

    private static Snapshot CreateSnapshot(string providerId, DateTimeOffset fetchedAtUtc)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = fetchedAtUtc,
            LimitWindows = []
        };
}
