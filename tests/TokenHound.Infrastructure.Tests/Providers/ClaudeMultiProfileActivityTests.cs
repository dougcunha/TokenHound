using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Claude;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Verifies isolated Claude activity reaches the provider-scoped event.</summary>
public sealed class ClaudeMultiProfileActivityTests
{

    /// <summary>Verifies a live session in a custom directory keeps its profile identity through activity polling.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithCustomDirectory_PublishesProfileActivity()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), $"claude_custom_sess_{Guid.NewGuid():N}");
        var sessionsDir = Path.Combine(tempDir, ".claude-work", "sessions");
        Directory.CreateDirectory(sessionsDir);

        try
        {

            await WriteLiveSessionAsync(sessionsDir, TestContext.Current.CancellationToken);

            var monitor = new ClaudeSessionMonitor(providerId: "claude-work", sessionsDirectory: sessionsDir);
            var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);
            AssertLiveSession(session);

            var activity = await ObserveActivityAsync(monitor, TestContext.Current.CancellationToken);
            activity.ProviderId.Should().Be("claude-work");
            AssertLiveSession(activity.AgentSession);
        }
        finally
        {

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static Task WriteLiveSessionAsync(string sessionsDir, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(
            Path.Combine(sessionsDir, "session.json"),
            $$"""{"pid": {{Environment.ProcessId}}}""",
            cancellationToken
        );

    private static void AssertLiveSession(AgentSession? session)
    {

        session.Should().NotBeNull();
        session!.Pid.Should().Be(Environment.ProcessId);
        session.State.Should().Be(AgentSessionState.Busy);
    }

    private static async Task<ProviderActivityChangedEventArgs> ObserveActivityAsync(
        ClaudeSessionMonitor monitor,
        CancellationToken cancellationToken)
    {

        using var store = new UsageStore(activityPollInterval: TimeSpan.FromMilliseconds(10));
        var eventSource = new TaskCompletionSource<ProviderActivityChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        store.ActivityUpdated += (_, args) => eventSource.TrySetResult(args);
        store.RegisterActivityMonitor(monitor);
        store.Start(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(10));

        var activity = await eventSource.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        await store.StopAsync(cancellationToken);

        return activity;
    }
}
