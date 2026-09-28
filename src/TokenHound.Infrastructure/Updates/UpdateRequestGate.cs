using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Persisted rate-limit gate for GitHub release checks: refuses dispatch before a stored deadline and records 429/403 limits.
/// </summary>
public sealed class UpdateRequestGate : IDisposable
{
    private static readonly ILogger LOGGER = Log.ForContext<UpdateRequestGate>();

    private readonly UpdateStateStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly SemaphoreSlim _gateLock = new(1, 1);

    private UpdateState _state;
    private bool _isProcessBlocked;
    private bool _disposed;

    /// <summary>
    /// Initializes a gate from the persisted update state.
    /// </summary>
    /// <param name="store">The update state store.</param>
    /// <param name="timeProvider">An optional clock; defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="rateLimitPolicy">An optional retry policy; defaults to the 60-second floor.</param>
    public UpdateRequestGate(
        UpdateStateStore store,
        TimeProvider? timeProvider = null,
        RateLimitPolicy? rateLimitPolicy = null)
    {

        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
        _state = store.Load();
    }

    /// <summary>Gets a value indicating whether an update check may be dispatched now.</summary>
    public bool CanDispatch
        => !_isProcessBlocked && RateLimitPolicy.CanDispatch(_timeProvider.GetUtcNow(), _state.DeadlineUtc);

    /// <summary>Gets the active rate-limit deadline, if any.</summary>
    public DateTimeOffset? ActiveDeadlineUtc
        => _state.DeadlineUtc;

    /// <summary>Gets the UTC time of the last completed check, if any.</summary>
    public DateTimeOffset? LastCheckUtc
        => _state.LastCheckUtc;

    /// <summary>Gets the count of consecutive rate-limit responses.</summary>
    public int ConsecutiveFailures
        => _state.ConsecutiveFailures;

    /// <summary>Gets a value indicating whether checks are disabled for this process after a persistence failure.</summary>
    public bool IsProcessBlocked
        => _isProcessBlocked;

    /// <summary>
    /// Records a 429, or a 403 with an exhausted quota, and persists the resulting deadline.
    /// </summary>
    /// <param name="retryAfterSeconds">The <c>Retry-After</c> seconds, if the response carried one.</param>
    /// <param name="resetUtc">The <c>X-RateLimit-Reset</c> instant, if the response carried one.</param>
    /// <param name="cancellationToken">Token cancelling the wait for the gate.</param>
    /// <returns>A task completing after the deadline is persisted.</returns>
    /// <exception cref="UpdateStatePersistenceException">The deadline could not be persisted; checks are disabled for the process.</exception>
    public async Task RecordRateLimitAsync(
        int? retryAfterSeconds,
        DateTimeOffset? resetUtc,
        CancellationToken cancellationToken = default)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            var failures = _state.ConsecutiveFailures + 1;
            var deadline = ComputeDeadline(retryAfterSeconds, resetUtc, failures);

            _state = _state with { DeadlineUtc = deadline, ConsecutiveFailures = failures };
            LOGGER.Warning("UpdateRateLimited {DeadlineUtc} {Failures}", deadline, failures);
            await PersistDeadlineOrBlockAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _gateLock.Release();
        }
    }

    /// <summary>
    /// Records a completed check: clears the deadline and failure streak and stores the check time. Persistence is best effort.
    /// </summary>
    /// <param name="cancellationToken">Token cancelling the wait for the gate.</param>
    /// <returns>A task completing after the state was updated.</returns>
    public async Task RecordSuccessAsync(CancellationToken cancellationToken = default)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            _state = new UpdateState { LastCheckUtc = _timeProvider.GetUtcNow() };
            await _store.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            LOGGER.Warning(ex, "Failed to persist update check state to {FilePath}", _store.FilePath);
        }
        finally
        {

            _gateLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _gateLock.Dispose();
    }

    private DateTimeOffset ComputeDeadline(int? retryAfterSeconds, DateTimeOffset? resetUtc, int failures)
    {

        var nowUtc = _timeProvider.GetUtcNow();
        var deadline = _rateLimitPolicy.CalculateDeadline(nowUtc, retryAfterSeconds, failures);

        if (resetUtc is { } reset && reset > deadline)
            deadline = reset;

        if (_state.DeadlineUtc is { } active && active > deadline)
            deadline = active;

        return deadline;
    }

    private async Task PersistDeadlineOrBlockAsync(CancellationToken cancellationToken)
    {
        try
        {

            await _store.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {

            _isProcessBlocked = true;

            throw new UpdateStatePersistenceException(
                "Failed to persist the update-check rate-limit deadline. Update checks are disabled for this process.",
                ex
            );
        }
    }
}
