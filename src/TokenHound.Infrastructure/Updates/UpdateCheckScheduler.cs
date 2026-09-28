using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Runs periodic update checks: the first evaluation shortly after startup, then one every few minutes,
/// checking only when <see cref="UpdatePolicy.IsCheckDue"/> says so and re-reading settings on every tick.
/// </summary>
public sealed class UpdateCheckScheduler
{
    /// <summary>The delay before the first evaluation after startup.</summary>
    public static readonly TimeSpan INITIAL_DELAY = TimeSpan.FromSeconds(60);

    /// <summary>The delay between evaluations.</summary>
    public static readonly TimeSpan TICK_INTERVAL = TimeSpan.FromMinutes(5);

    private static readonly ILogger LOGGER = Log.ForContext<UpdateCheckScheduler>();

    private readonly Func<UpdateSettings> _loadSettings;
    private readonly Func<DateTimeOffset?> _lastCheckUtc;
    private readonly Func<CancellationToken, Task<UpdateCheckOutcome>> _check;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCheckScheduler"/> class.
    /// </summary>
    /// <param name="loadSettings">Reads the current update settings; called on every tick.</param>
    /// <param name="lastCheckUtc">Reads the UTC time of the last completed check, or <see langword="null"/>.</param>
    /// <param name="check">Runs a scheduled update check.</param>
    /// <param name="timeProvider">An optional clock; defaults to <see cref="TimeProvider.System"/>.</param>
    public UpdateCheckScheduler(
        Func<UpdateSettings> loadSettings,
        Func<DateTimeOffset?> lastCheckUtc,
        Func<CancellationToken, Task<UpdateCheckOutcome>> check,
        TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(loadSettings);
        ArgumentNullException.ThrowIfNull(lastCheckUtc);
        ArgumentNullException.ThrowIfNull(check);

        _loadSettings = loadSettings;
        _lastCheckUtc = lastCheckUtc;
        _check = check;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Raised, on the calling worker thread, when a scheduled check finds an available update that was not skipped.
    /// </summary>
    public event EventHandler<UpdateCheckOutcome>? UpdateAvailable;

    /// <summary>
    /// Runs the schedule until <paramref name="cancellationToken"/> is cancelled; cancellation ends it quietly.
    /// </summary>
    /// <param name="cancellationToken">The token that stops the schedule.</param>
    /// <returns>A task that completes when the schedule stops.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {

        try
        {

            await Task.Delay(INITIAL_DELAY, _timeProvider, cancellationToken).ConfigureAwait(false);

            while (true)
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(TICK_INTERVAL, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

            LOGGER.Information("Update check scheduler stopped");
        }
    }

    /// <summary>
    /// Evaluates the schedule once and runs a check when one is due.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the check.</param>
    /// <returns><see langword="true"/> when a check ran; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> TickAsync(CancellationToken cancellationToken)
    {

        try
        {

            var settings = _loadSettings();

            if (!UpdatePolicy.IsCheckDue(_timeProvider.GetUtcNow(), _lastCheckUtc(), settings.IntervalHours, settings.IsEnabled))
                return false;

            var outcome = await _check(cancellationToken).ConfigureAwait(false);

            if (outcome.Status == UpdateCheckStatus.Available)
                UpdateAvailable?.Invoke(this, outcome);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            LOGGER.Error(ex, "Scheduled update check failed unexpectedly");

            return false;
        }
    }
}
