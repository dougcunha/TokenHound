using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Copilot;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies Copilot HTTP request gate serialization, rate-limit persistence, backoff, and recovery.
/// </summary>
public sealed partial class CopilotRequestGateTests
{
    private static readonly DateTimeOffset BASE_TIME = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that an unconstrained gate allows dispatch, and future deadlines block dispatch.</summary>
    [Fact]
    public void CanDispatch_EvaluatesDeadlinesCorrectly()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time);

            gate.CanDispatch.Should().BeTrue();
            gate.ActiveDeadlineUtc.Should().BeNull();
            gate.ConsecutiveFailures.Should().Be(0);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that SendAsync throws RateLimitBlockedException when blocked without calling action.</summary>
    [Fact]
    public async Task SendAsync_WhenBlockedByDeadline_ThrowsWithoutExecutingAction()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var future = BASE_TIME.AddMinutes(5);
            await archive.SaveCopilotHttpDeadlineAsync(future, 1, TestContext.Current.CancellationToken);

            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time);
            var executed = false;

            var act = () => gate.SendAsync<string>(_ =>
            {
                executed = true;
                return Task.FromResult("ok");
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<RateLimitBlockedException>();
            executed.Should().BeFalse();
            gate.CanDispatch.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that a 429 with Retry-After: 0 enforces the 60s minimum floor.</summary>
    [Fact]
    public async Task SendAsync_On429WithRetryAfterZero_EnforcesMinimumFloor()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time, jitterFactor: 0.0);

            var act = () => gate.SendAsync<string>(_ =>
                throw new CopilotApiException("Rate limited", HttpStatusCode.TooManyRequests, 0),
                TestContext.Current.CancellationToken
            );

            await act.Should().ThrowAsync<CopilotApiException>();

            gate.ConsecutiveFailures.Should().Be(1);
            gate.ActiveDeadlineUtc.Should().NotBeNull();
            gate.ActiveDeadlineUtc!.Value.Should().BeOnOrAfter(BASE_TIME.AddSeconds(60));

            var loaded = archive.LoadCopilotHttpDeadline();
            loaded.DeadlineUtc.Should().Be(gate.ActiveDeadlineUtc);
            loaded.ConsecutiveFailures.Should().Be(1);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that subsequent 429 with smaller penalty never lowers the active deadline.</summary>
    [Fact]
    public async Task SendAsync_SubsequentRateLimit_NeverLowersActiveDeadline()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time, jitterFactor: 0.0);

            await gate.RecordRateLimitAsync(300, TestContext.Current.CancellationToken);
            var initialDeadline = gate.ActiveDeadlineUtc;
            initialDeadline.Should().Be(BASE_TIME.AddSeconds(300));

            await gate.RecordRateLimitAsync(10, TestContext.Current.CancellationToken);
            gate.ActiveDeadlineUtc.Should().Be(initialDeadline);
            gate.ConsecutiveFailures.Should().Be(2);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that persistence failure blocks further sends for the process lifetime.</summary>
    [Fact]
    public async Task SendAsync_WhenDeadlinePersistenceFails_DisablesProcessLifetime()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time, jitterFactor: 0.0);

            Directory.CreateDirectory(dir);
            File.WriteAllText(archive.StatePath, "{}");
            File.SetAttributes(archive.StatePath, FileAttributes.ReadOnly);

            try
            {
                var act = () => gate.RecordRateLimitAsync(60, TestContext.Current.CancellationToken);
                var ex = await act.Should().ThrowAsync<RateLimitBlockedException>();
                ex.Which.IsPersistenceFailure.Should().BeTrue();

                gate.IsProcessBlocked.Should().BeTrue();
                gate.CanDispatch.Should().BeFalse();

                var sendAct = () => gate.SendAsync<string>(_ => Task.FromResult("fail"), TestContext.Current.CancellationToken);
                var sendEx = await sendAct.Should().ThrowAsync<RateLimitBlockedException>();
                sendEx.Which.IsPersistenceFailure.Should().BeTrue();
            }
            finally
            {
                File.SetAttributes(archive.StatePath, FileAttributes.Normal);
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string CreateDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "TokenHound", $"gate-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
