using Serilog;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Runs update checks for the running version: honors the persisted rate-limit gate, queries GitHub,
/// applies <see cref="UpdatePolicy"/>, and shares one in-flight check between concurrent callers.
/// </summary>
public sealed class UpdateCheckService : IDisposable
{
    private static readonly ILogger LOGGER = Log.ForContext<UpdateCheckService>();

    private readonly GitHubReleaseClient _client;
    private readonly UpdateRequestGate _gate;
    private readonly UpdateSettingsStore _settingsStore;
    private readonly ReleaseVersion? _currentVersion;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Lock _sync = new();

    private Task<UpdateCheckOutcome>? _inFlight;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCheckService"/> class.
    /// </summary>
    /// <param name="client">The GitHub release client.</param>
    /// <param name="gate">The persisted rate-limit gate.</param>
    /// <param name="settingsStore">The settings store providing the skipped version.</param>
    /// <param name="currentVersion">The running version, or <see langword="null"/> when it is not a release version.</param>
    public UpdateCheckService(
        GitHubReleaseClient client,
        UpdateRequestGate gate,
        UpdateSettingsStore settingsStore,
        ReleaseVersion? currentVersion)
    {

        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(settingsStore);

        _client = client;
        _gate = gate;
        _settingsStore = settingsStore;
        _currentVersion = currentVersion;
    }

    /// <summary>
    /// Gets the running version, or <see langword="null"/> when it is not a release version.
    /// </summary>
    public ReleaseVersion? CurrentVersion
        => _currentVersion;

    /// <summary>
    /// Runs an update check, or joins the one already running.
    /// </summary>
    /// <param name="trigger">What started the check.</param>
    /// <param name="cancellationToken">Token that stops this caller from waiting; the shared check keeps running.</param>
    /// <returns>The check outcome; failures are reported as outcomes, not exceptions.</returns>
    public Task<UpdateCheckOutcome> CheckAsync(UpdateCheckTrigger trigger, CancellationToken cancellationToken = default)
    {

        ObjectDisposedException.ThrowIf(_lifetime.IsCancellationRequested, this);

        lock (_sync)
        {
            if (_inFlight is null || _inFlight.IsCompleted)
                _inFlight = RunCheckAsync(trigger, _lifetime.Token);

            return _inFlight.WaitAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_lifetime.IsCancellationRequested)
            return;

        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task<UpdateCheckOutcome> RunCheckAsync(UpdateCheckTrigger trigger, CancellationToken cancellationToken)
    {

        LOGGER.Information("UpdateCheckStarted {Trigger}", trigger);

        var outcome = await EvaluateAsync(cancellationToken).ConfigureAwait(false);

        LOGGER.Information(
            "UpdateCheckCompleted {Outcome} {CurrentVersion} {LatestVersion}",
            outcome.Status,
            _currentVersion?.ToString(),
            outcome.LatestVersion?.ToString()
        );

        return outcome;
    }

    private async Task<UpdateCheckOutcome> EvaluateAsync(CancellationToken cancellationToken)
    {

        if (_currentVersion is null)
            return Outcome(UpdateCheckStatus.Unavailable, "The running build has no release version.");

        if (!_gate.CanDispatch)
            return RateLimitedOutcome();

        try
        {

            var release = await _client.GetLatestAsync(cancellationToken).ConfigureAwait(false);
            await _gate.RecordSuccessAsync(cancellationToken).ConfigureAwait(false);
            var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            return UpdatePolicy.Evaluate(_currentVersion, release, settings.SkippedVersion);
        }
        catch (UpdateRateLimitedException ex)
        {

            return await RecordRateLimitAsync(ex, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsCheckFailure(ex, cancellationToken))
        {

            LOGGER.Warning(ex, "Update check failed");

            return Outcome(UpdateCheckStatus.Failed, ex.Message);
        }
    }

    private async Task<UpdateCheckOutcome> RecordRateLimitAsync(UpdateRateLimitedException exception, CancellationToken cancellationToken)
    {
        try
        {

            await _gate.RecordRateLimitAsync(exception.RetryAfterSeconds, exception.ResetUtc, cancellationToken).ConfigureAwait(false);
        }
        catch (UpdateStatePersistenceException ex)
        {

            LOGGER.Error(ex, "Update checks disabled for this process");
        }

        return RateLimitedOutcome();
    }

    private UpdateCheckOutcome RateLimitedOutcome()
        => new()
        {
            Status = UpdateCheckStatus.RateLimited,
            RetryAfterUtc = _gate.IsProcessBlocked ? null : _gate.ActiveDeadlineUtc,
            Reason = _gate.IsProcessBlocked
                ? "Update checks are disabled until TokenHound restarts."
                : "GitHub rate limit reached."
        };

    private static UpdateCheckOutcome Outcome(UpdateCheckStatus status, string reason)
        => new() { Status = status, Reason = reason };

    private static bool IsCheckFailure(Exception exception, CancellationToken cancellationToken)
        => exception is HttpRequestException or JsonException or NotSupportedException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
