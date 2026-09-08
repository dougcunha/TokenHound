using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Copilot;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

public sealed partial class CopilotRequestGateTests
{
    /// <summary>Verifies that restarting the gate recovers persisted deadline and failure count.</summary>
    [Fact]
    public async Task Restart_HonorsPersistedDeadlineAndStreak()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);

            using (var gate1 = new CopilotRequestGate(archive, time, jitterFactor: 0.0))
            {
                await gate1.RecordRateLimitAsync(120, TestContext.Current.CancellationToken);
                gate1.ConsecutiveFailures.Should().Be(1);
            }

            using (var gate2 = new CopilotRequestGate(archive, time, jitterFactor: 0.0))
            {
                gate2.ConsecutiveFailures.Should().Be(1);
                gate2.ActiveDeadlineUtc.Should().Be(BASE_TIME.AddSeconds(120));
                gate2.CanDispatch.Should().BeFalse();
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that legacy backoffUntil.copilot is conservatively honored on restart.</summary>
    [Fact]
    public async Task Restart_HonorsLegacyBackoffUntilCopilot()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            Directory.CreateDirectory(dir);
            var legacy = BASE_TIME.AddMinutes(10);
            await File.WriteAllTextAsync(
                archive.StatePath,
                $"{{\"backoffUntil\":{{\"copilot\":\"{legacy:O}\"}}}}",
                TestContext.Current.CancellationToken
            );

            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time);

            gate.ActiveDeadlineUtc.Should().Be(legacy);
            gate.ConsecutiveFailures.Should().Be(1);
            gate.CanDispatch.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that success after deadline resets failure streak and clears deadline.</summary>
    [Fact]
    public async Task SendAsync_SuccessAfterDeadline_ResetsStreakAndClearsState()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time, jitterFactor: 0.0);

            await gate.RecordRateLimitAsync(60, TestContext.Current.CancellationToken);
            gate.ConsecutiveFailures.Should().Be(1);

            time.Advance(TimeSpan.FromSeconds(65));
            gate.CanDispatch.Should().BeTrue();

            var result = await gate.SendAsync(_ => Task.FromResult("ok"), TestContext.Current.CancellationToken);
            result.Should().Be("ok");

            gate.ConsecutiveFailures.Should().Be(0);
            gate.ActiveDeadlineUtc.Should().BeNull();

            var loaded = archive.LoadCopilotHttpDeadline();
            loaded.DeadlineUtc.Should().BeNull();
            loaded.ConsecutiveFailures.Should().Be(0);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that HttpResponseMessage with 429 triggers rate limiting.</summary>
    [Fact]
    public async Task SendAsync_WhenHttpResponseIs429_RecordsRateLimit()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time, jitterFactor: 0.0);

            using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(90));

            var returned = await gate.SendAsync(_ => Task.FromResult(response), TestContext.Current.CancellationToken);
            returned.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            gate.ConsecutiveFailures.Should().Be(1);
            gate.ActiveDeadlineUtc.Should().Be(BASE_TIME.AddSeconds(90));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies that caller cancellation propagates without recording a rate limit.</summary>
    [Fact]
    public async Task SendAsync_WhenCancelled_DoesNotRecordRateLimit()
    {
        var dir = CreateDirectory();

        try
        {
            using var archive = new UsageArchive(dir);
            var time = new MutableTimeProvider(BASE_TIME);
            using var gate = new CopilotRequestGate(archive, time);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => gate.SendAsync<string>(_ => Task.FromResult("ok"), cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            gate.ConsecutiveFailures.Should().Be(0);
            gate.ActiveDeadlineUtc.Should().BeNull();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
