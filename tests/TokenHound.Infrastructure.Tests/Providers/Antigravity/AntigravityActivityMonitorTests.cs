using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityActivityMonitor"/>.
/// </summary>
public sealed class AntigravityActivityMonitorTests
{
    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public void ProviderId_IsGemini()
    {
        var monitor = new AntigravityActivityMonitor();

        Assert.Equal("gemini", monitor.ProviderId);
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenNoEndpointAndNoTranscripts_ReturnsNull()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => []);

        var reader = new AntigravityTranscriptReader(["C:\\nonexistent_dir_random_abc"]);
        var monitor = new AntigravityActivityMonitor(discovery, reader);

        // Act
        var session = await monitor.CheckLivenessAsync();

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenFileWrittenWithinThreshold_ReturnsBusy()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_act_busy_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "transcript.jsonl");
        await File.WriteAllTextAsync(filePath, "{}\n");

        var fileWriteTime = File.GetLastWriteTimeUtc(filePath);
        var now = fileWriteTime.AddSeconds(20); // 20s elapsed <= 45s threshold
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(now, TimeSpan.Zero));

        try
        {
            var discovery = new AntigravityEndpointDiscovery(
                processEnumerator: () => [],
                portResolver: _ => []);

            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            var monitor = new AntigravityActivityMonitor(discovery, reader, timeProvider);

            // Act
            var session = await monitor.CheckLivenessAsync();

            // Assert
            Assert.NotNull(session);
            Assert.Equal(AgentSessionState.Busy, session.State);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CheckLivenessAsync_WhenFileOlderThanThreshold_ReturnsIdle()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"gemini_act_idle_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "transcript.jsonl");
        await File.WriteAllTextAsync(filePath, "{}\n");

        var fileWriteTime = File.GetLastWriteTimeUtc(filePath);
        var now = fileWriteTime.AddSeconds(50); // 50s elapsed > 45s threshold
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(now, TimeSpan.Zero));

        try
        {
            var discovery = new AntigravityEndpointDiscovery(
                processEnumerator: () => [],
                portResolver: _ => []);

            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);
            var monitor = new AntigravityActivityMonitor(discovery, reader, timeProvider);

            // Act
            var session = await monitor.CheckLivenessAsync();

            // Assert
            Assert.NotNull(session);
            Assert.Equal(AgentSessionState.Idle, session.State);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CheckLivenessAsync_PropagatesCancellation()
    {
        // Arrange
        var monitor = new AntigravityActivityMonitor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await monitor.CheckLivenessAsync(cts.Token);
        });
    }
}
