using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Tests.ViewModels;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies gate use, policy evaluation, single-flight checks, and last-check bookkeeping of <see cref="UpdateCheckService"/> (TC-10, TC-11).
/// </summary>
public sealed class UpdateCheckServiceTests : IDisposable
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), $"update_check_test_{Guid.NewGuid():N}");
    private readonly ManualTimeProvider _clock = new(NOW);

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a newer release is available and the check time is recorded.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WithNewerRelease_ReturnsAvailableAndRecordsLastCheck()
    {

        using var service = CreateService(_ => GitHubReleaseClientTests.Json(HttpStatusCode.OK, GitHubReleaseClientTests.RELEASE_JSON), "1.2.3", out _);

        var outcome = await service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);

        outcome.Status.Should().Be(UpdateCheckStatus.Available);
        outcome.LatestVersion!.ToString().Should().Be("1.4.0");
        new UpdateStateStore(_directoryPath).Load().LastCheckUtc.Should().Be(NOW);
    }

    /// <summary>
    /// Verifies that the skipped version from settings is honored.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WithSkippedVersion_ReturnsSkipped()
    {

        Directory.CreateDirectory(_directoryPath);
        new UpdateSettingsStore(SettingsPath).Save(new UpdateSettings { SkippedVersion = "1.4.0" }).Should().BeTrue();
        using var service = CreateService(_ => GitHubReleaseClientTests.Json(HttpStatusCode.OK, GitHubReleaseClientTests.RELEASE_JSON), "1.2.3", out _);

        var outcome = await service.CheckAsync(UpdateCheckTrigger.Scheduled, TestContext.Current.CancellationToken);

        outcome.Status.Should().Be(UpdateCheckStatus.Skipped);
    }

    /// <summary>
    /// Verifies that two concurrent checks share one HTTP request and one outcome.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WhenConcurrent_SendsOneRequest()
    {

        var release = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHttpHandler(_ => release.Task);
        using var service = CreateService(handler, "1.2.3");

        var first = service.CheckAsync(UpdateCheckTrigger.Scheduled, TestContext.Current.CancellationToken);
        var second = service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);
        release.SetResult(GitHubReleaseClientTests.Json(HttpStatusCode.OK, GitHubReleaseClientTests.RELEASE_JSON));

        var outcomes = await Task.WhenAll(first, second);

        handler.Requests.Should().ContainSingle();
        outcomes[0].Should().BeSameAs(outcomes[1]);
    }

    /// <summary>
    /// Verifies that a 429 records a deadline and that the next check is refused without a request and without a new last check.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WhenRateLimited_RecordsDeadlineAndRefusesNextCheck()
    {

        var handler = new ScriptedHttpHandler(_ => TooManyRequests());
        using var service = CreateService(handler, "1.2.3");

        var limited = await service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);
        var refused = await service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);

        limited.Status.Should().Be(UpdateCheckStatus.RateLimited);
        limited.RetryAfterUtc.Should().BeOnOrAfter(NOW.AddSeconds(300));
        refused.Status.Should().Be(UpdateCheckStatus.RateLimited);
        handler.Requests.Should().ContainSingle();
        new UpdateStateStore(_directoryPath).Load().LastCheckUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a server error is reported as a failure without a deadline.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WhenServerError_ReturnsFailedWithoutDeadline()
    {

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.BadGateway), "1.2.3", out _);

        var outcome = await service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);

        outcome.Status.Should().Be(UpdateCheckStatus.Failed);
        new UpdateStateStore(_directoryPath).Load().DeadlineUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an HTTP timeout or a network failure is reported as a failure without a deadline.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WhenTimeoutOrNetworkFailure_ReturnsFailed()
    {

        using var timedOut = CreateService(new ScriptedHttpHandler(_ => Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout"))), "1.2.3");
        using var offline = CreateService(new ScriptedHttpHandler(_ => Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"))), "1.2.3");

        var timeoutOutcome = await timedOut.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);
        var offlineOutcome = await offline.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);

        timeoutOutcome.Status.Should().Be(UpdateCheckStatus.Failed);
        offlineOutcome.Status.Should().Be(UpdateCheckStatus.Failed);
        new UpdateStateStore(_directoryPath).Load().DeadlineUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a build without a release version never queries GitHub.
    /// </summary>
    [Fact]
    public async Task CheckAsync_WithoutCurrentVersion_ReturnsUnavailableWithoutRequest()
    {

        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK), "Version unavailable", out var handler);

        var outcome = await service.CheckAsync(UpdateCheckTrigger.Manual, TestContext.Current.CancellationToken);

        outcome.Status.Should().Be(UpdateCheckStatus.Unavailable);
        handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the informational version drops build metadata.
    /// </summary>
    [Fact]
    public void RunningVersion_FromText_DropsBuildMetadata()
    {

        RunningVersion.FromText("1.4.0+4f2a9c1").Should().Be(new ReleaseVersion { Major = 1, Minor = 4, Patch = 0 });
        RunningVersion.FromText("Version unavailable").Should().BeNull();
    }

    private string SettingsPath
        => Path.Combine(_directoryPath, "settings.json");

    private UpdateCheckService CreateService(Func<HttpRequestMessage, HttpResponseMessage> script, string version, out ScriptedHttpHandler handler)
    {

        handler = new ScriptedHttpHandler(script);

        return CreateService(handler, version);
    }

    private UpdateCheckService CreateService(ScriptedHttpHandler handler, string version)
        => new(
            new GitHubReleaseClient("1.2.3", new HttpClient(handler), _clock),
            new UpdateRequestGate(new UpdateStateStore(_directoryPath), _clock),
            new UpdateSettingsStore(SettingsPath),
            RunningVersion.FromText(version)
        );

    private static HttpResponseMessage TooManyRequests()
    {

        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.Add("Retry-After", "300");

        return response;
    }
}
