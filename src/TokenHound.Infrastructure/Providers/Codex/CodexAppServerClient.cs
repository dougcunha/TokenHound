using Serilog;
using System.ComponentModel;
using System.Text.Json;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>Owns a reusable Codex app-server session over stdio JSON-RPC.</summary>
public sealed class CodexAppServerClient : IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);
    private readonly Func<string?> _executableResolver;
    private readonly ICodexProcessFactory _processFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeout;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _disposeSync = new();
    private ICodexProcess? _process;
    private Task? _disposeTask;
    private DateTimeOffset _restartAfter;
    private int _failures;
    private int _requestId = 1;
    private int _disposed;

    /// <summary>Initializes a client using operating system process ownership.</summary>
    public CodexAppServerClient()
        : this(new SystemCodexProcessFactory(), static () => FindExecutable())
    {
    }

    /// <summary>Initializes a client with an injectable process factory.</summary>
    public CodexAppServerClient(ICodexProcessFactory processFactory, TimeSpan? timeout = null)
        : this(processFactory, static () => FindExecutable(), timeout)
    {
    }

    /// <summary>Initializes injectable process discovery, watchdog, and restart clock.</summary>
    public CodexAppServerClient(
        ICodexProcessFactory processFactory,
        Func<string?> executableResolver,
        TimeSpan? timeout = null,
        TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(executableResolver);

        if (timeout is not null && timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        _processFactory = processFactory;
        _executableResolver = executableResolver;
        _timeout = timeout ?? DEFAULT_TIMEOUT;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Resolves Codex from standard Windows locations and PATH.</summary>
    public static string? FindExecutable(
        string? localAppDataDirectory = null,
        string? programFilesDirectory = null,
        string? userProfileDirectory = null,
        string? pathEnvironment = null)
        => CodexExecutable.Find(
            localAppDataDirectory,
            programFilesDirectory,
            userProfileDirectory,
            pathEnvironment
        );

    /// <summary>Reads limits using one serialized request on the retained server.</summary>
    public async Task<CodexRateLimitsDto?> GetRateLimitsAsync(CancellationToken cancellationToken = default)
    {

        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _gate.WaitAsync(linked.Token).ConfigureAwait(false);

        try
        {

            using var timeout = new CancellationTokenSource(_timeout);
            using var request = CancellationTokenSource.CreateLinkedTokenSource(linked.Token, timeout.Token);

            return await QueryAsync(request.Token, linked.Token).ConfigureAwait(false);
        }
        finally
        {

            _gate.Release();
        }
    }

    private async Task<CodexRateLimitsDto?> QueryAsync(CancellationToken requestToken, CancellationToken callerToken)
    {

        try
        {

            return await QuerySessionAsync(requestToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

            await RetireSessionAsync().ConfigureAwait(false);
            ScheduleRestart();
            callerToken.ThrowIfCancellationRequested();

            return null;
        }
        catch (Exception ex) when (ex is IOException or Win32Exception or InvalidOperationException or JsonException)
        {

            Log.Debug(ex, "Codex app-server session failed");
            await RetireSessionAsync().ConfigureAwait(false);
            ScheduleRestart();

            return null;
        }
    }

    private async Task<CodexRateLimitsDto?> QuerySessionAsync(CancellationToken cancellationToken)
    {

        var process = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);

        if (process is null)
            return null;

        var limits = await CodexServerProtocol.ReadLimitsAsync(process, ++_requestId, cancellationToken).ConfigureAwait(false);
        _failures = 0;

        return limits;
    }

    private async Task<ICodexProcess?> EnsureSessionAsync(CancellationToken cancellationToken)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var existing = _process;

        if (existing is not null)
        {

            if (!existing.HasExited)
                return existing;

            throw new IOException("Codex app-server exited between queries.");
        }

        if (_timeProvider.GetUtcNow() < _restartAfter)
            return null;

        var executable = _executableResolver();

        if (string.IsNullOrWhiteSpace(executable))
            return null;

        var process = _processFactory.Start(executable, "app-server");
        _process = process;
        await CodexServerProtocol.InitializeAsync(process, cancellationToken).ConfigureAwait(false);

        return process;
    }

    private void ScheduleRestart()
    {

        _failures = Math.Min(_failures + 1, 5);
        var delay = TimeSpan.FromSeconds(Math.Min(5 * (1 << (_failures - 1)), 60));
        _restartAfter = _timeProvider.GetUtcNow() + delay;
        Log.Debug("Codex app-server restart deferred for {RestartDelay}", delay);
    }

    private async ValueTask RetireSessionAsync()
    {

        var process = Interlocked.Exchange(ref _process, null);

        if (process is not null)
            await process.StopAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        Interlocked.Exchange(ref _disposed, 1);
        _lifetime.Cancel();
        var process = Interlocked.Exchange(ref _process, null);

        if (process is null)
            return;

        process.Kill();
        process.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {

        lock (_disposeSync)
        {

            Interlocked.Exchange(ref _disposed, 1);
            _lifetime.Cancel();

            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {

        await _gate.WaitAsync().ConfigureAwait(false);

        try
        {

            await RetireSessionAsync().ConfigureAwait(false);
        }
        finally
        {

            _gate.Release();
        }
    }

    /// <summary>Starts owned Codex app-server processes.</summary>
    public interface ICodexProcessFactory
    {
        /// <summary>Starts a process for the executable and arguments.</summary>
        ICodexProcess Start(string executablePath, string arguments);
    }

    /// <summary>Represents redirected streams and the app-server process lifecycle.</summary>
    public interface ICodexProcess : IDisposable
    {
        /// <summary>Gets the redirected standard input writer.</summary>
        TextWriter StandardInput { get; }

        /// <summary>Gets the redirected standard output reader.</summary>
        TextReader StandardOutput { get; }

        /// <summary>Gets whether the process has exited.</summary>
        bool HasExited { get; }

        /// <summary>Terminates the entire process tree.</summary>
        void Kill();

        /// <summary>Stops and disposes the process; OS implementations allow graceful exit.</summary>
        ValueTask StopAsync()
        {

            Kill();
            Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
