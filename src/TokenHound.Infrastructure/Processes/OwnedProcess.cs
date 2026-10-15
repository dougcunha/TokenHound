using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace TokenHound.Infrastructure.Processes;

internal sealed class OwnedProcess : IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan EXIT_TIMEOUT = TimeSpan.FromSeconds(3);
    private readonly IDisposable? _job;
    private readonly Process _process;
    private readonly TextReader _standardError;
    private readonly Task _errorDrain;
    private readonly object _sync = new();
    private Task? _stopTask;

    internal OwnedProcess(Process process, TextWriter input, TextReader output, TextReader error, IDisposable? job = null)
    {

        _process = process;
        StandardInput = input;
        StandardOutput = output;
        _standardError = error;
        _job = job;
        _errorDrain = DrainErrorAsync();
    }

    internal TextWriter StandardInput { get; }

    internal TextReader StandardOutput { get; }

    internal bool HasExited
        => _process.HasExited;

    internal int ExitCode
        => _process.ExitCode;

    internal int Id
        => _process.Id;

    internal static OwnedProcess Start(string executable, string arguments)
    {

        if (OperatingSystem.IsWindows())
            return WindowsProcessLauncher.Start(executable, arguments);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        if (!process.Start())
            throw new InvalidOperationException("The background CLI process could not be started.");

        return new OwnedProcess(
            process,
            process.StandardInput,
            process.StandardOutput,
            process.StandardError
        );
    }

    internal Task WaitForExitAsync(CancellationToken cancellationToken)
        => _process.WaitForExitAsync(cancellationToken);

    internal void Kill()
    {

        try
        {

            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {

            Log.Debug(ex, "Background CLI termination raced with process exit");
        }
        finally
        {

            _job?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        Kill();
        DisposeStreams();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {

        lock (_sync)
            return new ValueTask(_stopTask ??= StopAsync());
    }

    private async Task StopAsync()
    {

        try
        {

            CloseInput();
            using var timeout = new CancellationTokenSource(EXIT_TIMEOUT);
            await _process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or InvalidOperationException or Win32Exception)
        {

            Log.Debug(ex, "Background CLI did not finish graceful shutdown");
        }
        finally
        {

            Kill();
            await _errorDrain.ConfigureAwait(false);
            DisposeStreams();
        }
    }

    private async Task DrainErrorAsync()
    {

        try
        {

            var buffer = new char[4096];

            while (await _standardError.ReadAsync(buffer.AsMemory()).ConfigureAwait(false) != 0)
            {

                // Discard CLI diagnostics without retaining borrowed credentials or filling the pipe.
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {

            Log.Debug(ex, "Background CLI diagnostic stream closed");
        }
    }

    private void DisposeStreams()
    {

        CloseInput();
        StandardOutput.Dispose();
        _standardError.Dispose();
        _process.Dispose();
    }

    private void CloseInput()
    {

        try
        {

            StandardInput.Dispose();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {

            Log.Debug(ex, "Background CLI input already closed");
        }
    }
}
