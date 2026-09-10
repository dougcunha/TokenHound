using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Serializes dispatch, evaluates deadlines, and updates rate limits for Copilot HTTP requests.
/// </summary>
public sealed class CopilotRequestGate : IDisposable
{
    private readonly UsageArchive _archive;
    private readonly TimeProvider _timeProvider;
    private readonly Random? _random;
    private readonly double? _jitterFactor;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly SemaphoreSlim _gateLock = new(1, 1);

    private DateTimeOffset? _activeDeadlineUtc;
    private int _consecutiveFailures;
    private bool _isProcessBlocked;
    private bool _disposed;

    /// <summary>
    /// Initializes a gate backed by the specified usage archive.
    /// </summary>
    /// <param name="archive">The usage archive for persisting gate state.</param>
    /// <param name="timeProvider">An optional time provider for testing.</param>
    /// <param name="random">An optional random generator for jitter.</param>
    /// <param name="jitterFactor">An optional fixed jitter factor for testing.</param>
    /// <param name="rateLimitPolicy">An optional isolated retry policy.</param>
    public CopilotRequestGate(
        UsageArchive archive,
        TimeProvider? timeProvider = null,
        Random? random = null,
        double? jitterFactor = null,
        RateLimitPolicy? rateLimitPolicy = null)
    {

        ArgumentNullException.ThrowIfNull(archive);
        _archive = archive;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _random = random;
        _jitterFactor = jitterFactor;
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();

        var loaded = archive.LoadCopilotHttpDeadline();
        _activeDeadlineUtc = loaded.DeadlineUtc;
        _consecutiveFailures = loaded.ConsecutiveFailures;
    }

    /// <summary>Gets a value indicating whether a request may currently be dispatched.</summary>
    public bool CanDispatch
        => !_isProcessBlocked && RateLimitPolicy.CanDispatch(_timeProvider.GetUtcNow(), _activeDeadlineUtc);

    /// <summary>Gets the currently active UTC rate-limit deadline, if any.</summary>
    public DateTimeOffset? ActiveDeadlineUtc
        => _activeDeadlineUtc;

    /// <summary>Gets the count of consecutive rate-limit failures.</summary>
    public int ConsecutiveFailures
        => _consecutiveFailures;

    /// <summary>Gets a value indicating whether Copilot sends are disabled for the process lifetime.</summary>
    public bool IsProcessBlocked
        => _isProcessBlocked;

    /// <summary>
    /// Executes an asynchronous request action through the gate, serializing dispatch and handling rate limits.
    /// </summary>
    public async Task<T> SendAsync<T>(
        Func<CancellationToken, Task<T>> sendAction,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(sendAction);
        ThrowIfDisposed();
        await _gateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            EnsureCanDispatch();
            var result = await ExecuteGuardedSendAsync(sendAction, cancellationToken).ConfigureAwait(false);
            await HandleDispatchSuccessAsync(result, cancellationToken).ConfigureAwait(false);

            return result;
        }
        finally
        {

            _gateLock.Release();
        }
    }

    /// <summary>
    /// Explicitly records a rate-limit condition with optional Retry-After seconds.
    /// </summary>
    public async Task RecordRateLimitAsync(
        int? retryAfterSeconds,
        CancellationToken cancellationToken = default)
    {

        ThrowIfDisposed();
        await _gateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            await HandleRateLimitAsync(retryAfterSeconds, cancellationToken).ConfigureAwait(false);
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

    private void EnsureCanDispatch()
    {

        if (_isProcessBlocked)
        {
            throw new RateLimitBlockedException(
                _activeDeadlineUtc ?? DateTimeOffset.MaxValue,
                null,
                true,
                "Copilot HTTP requests are disabled for this process due to a rate-limit persistence failure."
            );
        }

        var nowUtc = _timeProvider.GetUtcNow();

        if (!RateLimitPolicy.CanDispatch(nowUtc, _activeDeadlineUtc))
        {
            var remaining = (int)Math.Ceiling((_activeDeadlineUtc!.Value - nowUtc).TotalSeconds);

            throw new RateLimitBlockedException(_activeDeadlineUtc.Value, Math.Max(0, remaining), false);
        }
    }

    private async Task<T> ExecuteGuardedSendAsync<T>(
        Func<CancellationToken, Task<T>> sendAction,
        CancellationToken cancellationToken)
    {

        try
        {

            return await sendAction(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {

            if (CopilotRateLimitExtractor.TryExtractRateLimit(ex, out var retryAfter))
                await HandleRateLimitAsync(retryAfter, cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    private DateTimeOffset ComputeNextDeadline(DateTimeOffset nowUtc, int? retryAfterSeconds)
    {

        var calculated = _jitterFactor.HasValue
            ? _rateLimitPolicy.CalculateDeadline(nowUtc, retryAfterSeconds, _consecutiveFailures, _jitterFactor.Value)
            : _rateLimitPolicy.CalculateDeadline(nowUtc, retryAfterSeconds, _consecutiveFailures, _random);

        return _activeDeadlineUtc.HasValue && _activeDeadlineUtc.Value > calculated
            ? _activeDeadlineUtc.Value
            : calculated;
    }

    private async Task PersistDeadlineOrFailAsync(
        DateTimeOffset deadline,
        int? retryAfterSeconds,
        CancellationToken cancellationToken)
    {

        try
        {

            await _archive.SaveCopilotHttpDeadlineAsync(
                deadline,
                _consecutiveFailures,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {

            _isProcessBlocked = true;

            throw new RateLimitBlockedException(
                deadline,
                retryAfterSeconds,
                true,
                "Failed to persist Copilot HTTP rate-limit deadline. Further sends are disabled for this process.",
                ex
            );
        }
    }

    private async Task HandleRateLimitAsync(
        int? retryAfterSeconds,
        CancellationToken cancellationToken)
    {

        var nowUtc = _timeProvider.GetUtcNow();
        _consecutiveFailures++;

        _activeDeadlineUtc = ComputeNextDeadline(nowUtc, retryAfterSeconds);
        await PersistDeadlineOrFailAsync(
            _activeDeadlineUtc.Value,
            retryAfterSeconds,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task HandleDispatchSuccessAsync<T>(
        T result,
        CancellationToken cancellationToken)
    {

        if (result is HttpResponseMessage response && response.StatusCode == HttpStatusCode.TooManyRequests)
        {

            var retryAfter = CopilotRateLimitExtractor.ExtractRetryAfterSeconds(response, _timeProvider);
            await HandleRateLimitAsync(retryAfter, cancellationToken).ConfigureAwait(false);

            return;
        }

        if (result is HttpResponseMessage nonOkResponse && !nonOkResponse.IsSuccessStatusCode)
            return;

        if (_consecutiveFailures > 0 || _activeDeadlineUtc.HasValue)
        {
            _consecutiveFailures = 0;
            _activeDeadlineUtc = null;

            try
            {

                await _archive.ClearCopilotHttpDeadlineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Clearing on success is best-effort; streak is already reset in memory.
            }
        }
    }

    private void ThrowIfDisposed()
    {

        if (_disposed)
            throw new ObjectDisposedException(nameof(CopilotRequestGate));
    }
}
