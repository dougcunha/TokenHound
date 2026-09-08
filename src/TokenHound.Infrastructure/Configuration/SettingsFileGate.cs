using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Serializes read-modify-write cycles of the application settings file across every store that writes it.
/// </summary>
internal static class SettingsFileGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> GATES
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Acquires exclusive access to a settings file, blocking until the current writer releases it.
    /// </summary>
    /// <param name="filePath">The settings file path to guard.</param>
    /// <returns>A token releasing the gate when disposed.</returns>
    internal static IDisposable Acquire(string filePath)
    {

        var gate = ResolveGate(filePath);

        gate.Wait();

        return new GateRelease(gate);
    }

    /// <summary>
    /// Asynchronously acquires exclusive access to a settings file.
    /// </summary>
    /// <param name="filePath">The settings file path to guard.</param>
    /// <param name="cancellationToken">Token cancelling the wait.</param>
    /// <returns>A token releasing the gate when disposed.</returns>
    internal static async Task<IDisposable> AcquireAsync(string filePath, CancellationToken cancellationToken)
    {

        var gate = ResolveGate(filePath);

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new GateRelease(gate);
    }

    private static SemaphoreSlim ResolveGate(string filePath)
        => GATES.GetOrAdd(NormalizePath(filePath), static _ => new SemaphoreSlim(1, 1));

    private static string NormalizePath(string filePath)
    {

        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        try
        {

            return Path.GetFullPath(filePath);
        }
        catch (Exception)
        {

            return filePath;
        }
    }

    private sealed class GateRelease : IDisposable
    {
        private readonly SemaphoreSlim _gate;

        private int _isReleased;

        /// <summary>
        /// Initializes a new instance of the <see cref="GateRelease"/> class.
        /// </summary>
        /// <param name="gate">The semaphore released on disposal.</param>
        internal GateRelease(SemaphoreSlim gate)
        {

            _gate = gate;
        }

        /// <inheritdoc />
        public void Dispose()
        {

            if (Interlocked.Exchange(ref _isReleased, 1) == 0)
                _gate.Release();
        }
    }
}
