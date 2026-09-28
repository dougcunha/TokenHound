using System;
using System.Threading;

namespace TokenHound.Infrastructure.System;

/// <summary>
/// Named per-session mutex held for the lifetime of a TokenHound process, so an updated executable and the installer
/// can wait for the previous instance to exit. It does not enforce a single instance.
/// </summary>
public sealed class ApplicationInstanceMutex : IDisposable
{
    /// <summary>
    /// The mutex name; unprefixed names live in the session (<c>Local\</c>) namespace, which the installer checks too.
    /// </summary>
    public const string DEFAULT_NAME = "TokenHound.App.Instance";

    private readonly Mutex _mutex;
    private bool _owned;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInstanceMutex"/> class without acquiring it.
    /// </summary>
    /// <param name="name">The mutex name; tests pass a unique name.</param>
    public ApplicationInstanceMutex(string name = DEFAULT_NAME)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _mutex = new Mutex(false, name);
    }

    /// <summary>
    /// Gets a value indicating whether this instance currently owns the mutex.
    /// </summary>
    public bool IsOwned
        => _owned;

    /// <summary>
    /// Tries to acquire the mutex, waiting up to <paramref name="timeout"/>; an abandoned mutex counts as acquired.
    /// Must be released on the same thread, which <see cref="Dispose"/> does.
    /// </summary>
    /// <param name="timeout">How long to wait; <see cref="TimeSpan.Zero"/> does not wait.</param>
    /// <returns><see langword="true"/> when the mutex is owned after the call.</returns>
    public bool TryAcquire(TimeSpan timeout)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_owned)
            return true;

        try
        {

            _owned = _mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {

            _owned = true;
        }

        return _owned;
    }

    /// <summary>
    /// Releases the mutex when owned and frees the handle.
    /// </summary>
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;

        if (_owned)
            _mutex.ReleaseMutex();

        _owned = false;
        _mutex.Dispose();
    }
}
