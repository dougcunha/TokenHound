using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>Communicates with the Codex app server over stdio JSON-RPC.</summary>
public sealed class CodexAppServerClient
{
    private const string APP_SERVER_ARGUMENTS = "app-server";
    private const string CODEX_EXECUTABLE_NAME = "codex.exe";
    private const string INITIALIZE_REQUEST = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"TokenHound\",\"title\":\"TokenHound\",\"version\":\"1.0\"}}}";
    private const string INITIALIZED_NOTIFICATION = "{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}";
    private const string RATE_LIMITS_REQUEST = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"account/rateLimits/read\",\"params\":null}";
    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);

    private readonly Func<string?> _executableResolver;
    private readonly ICodexProcessFactory _processFactory;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a client using the operating system process implementation.</summary>
    public CodexAppServerClient()
        : this(new SystemCodexProcessFactory(), static () => FindExecutable(), null)
    {
    }

    /// <summary>Initializes a client with an injectable process factory.</summary>
    public CodexAppServerClient(ICodexProcessFactory processFactory, TimeSpan? timeout = null)
        : this(processFactory, static () => FindExecutable(), timeout)
    {
    }

    /// <summary>Initializes a client with injectable process and executable discovery.</summary>
    public CodexAppServerClient(
        ICodexProcessFactory processFactory,
        Func<string?> executableResolver,
        TimeSpan? timeout = null)
    {

        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(executableResolver);

        if (timeout is not null && timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        _processFactory = processFactory;
        _executableResolver = executableResolver;
        _timeout = timeout ?? DEFAULT_TIMEOUT;
    }

    /// <summary>Resolves Codex from standard Windows locations and PATH.</summary>
    public static string? FindExecutable(
        string? localAppDataDirectory = null,
        string? programFilesDirectory = null,
        string? userProfileDirectory = null,
        string? pathEnvironment = null)
    {

        var localAppData = localAppDataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = programFilesDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var userProfile = userProfileDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = pathEnvironment ?? Environment.GetEnvironmentVariable("PATH");
        var candidates = new[]
        {
            Path.Combine(localAppData, Path.Combine("Programs", "ChatGPT", "resources"), CODEX_EXECUTABLE_NAME),
            Path.Combine(programFiles, Path.Combine("ChatGPT", "resources"), CODEX_EXECUTABLE_NAME),
            Path.Combine(userProfile, Path.Combine(".codex", "bin"), CODEX_EXECUTABLE_NAME)
        };

        foreach (var candidate in candidates)
        {

            if (File.Exists(candidate))
                return candidate;
        }

        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {

            var candidate = Path.Combine(directory, CODEX_EXECUTABLE_NAME);

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>Reads current primary and secondary rate limits.</summary>
    public async Task<CodexRateLimitsDto?> GetRateLimitsAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var executablePath = _executableResolver();

        if (string.IsNullOrWhiteSpace(executablePath))
            return null;

        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        ICodexProcess? process = null;

        try
        {

            process = _processFactory.Start(executablePath, APP_SERVER_ARGUMENTS);
            await SendHandshakeAsync(process, linkedSource.Token).ConfigureAwait(false);

            return await ReadRateLimitsAsync(process.StandardOutput, linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {

            return null;
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            return null;
        }
        finally
        {

            StopProcess(process);
        }
    }

    private static CodexRateLimitsDto? ParseRateLimitsResponse(string json)
    {

        try
        {

            var response = JsonSerializer.Deserialize<JsonRpcResponse>(json, SERIALIZER_OPTIONS);

            return response?.Id == 2 ? response.Result?.RateLimits : null;
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static async Task SendHandshakeAsync(ICodexProcess process, CancellationToken cancellationToken)
    {

        await SendRequestAsync(process.StandardInput, INITIALIZE_REQUEST, cancellationToken).ConfigureAwait(false);
        await SendRequestAsync(process.StandardInput, INITIALIZED_NOTIFICATION, cancellationToken).ConfigureAwait(false);
        await SendRequestAsync(process.StandardInput, RATE_LIMITS_REQUEST, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendRequestAsync(TextWriter writer, string request, CancellationToken cancellationToken)
    {

        await writer.WriteLineAsync(request).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CodexRateLimitsDto?> ReadRateLimitsAsync(TextReader reader, CancellationToken cancellationToken)
    {

        while (true)
        {

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line is null)
                return null;

            var rateLimits = ParseRateLimitsResponse(line);

            if (rateLimits is not null)
                return rateLimits;
        }
    }

    private static void StopProcess(ICodexProcess? process)
    {

        if (process is null)
            return;

        try
        {

            if (!process.HasExited)
                process.Kill();
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {

            process.Dispose();
        }
    }

    private sealed record JsonRpcResponse(int Id, JsonRpcResult? Result);

    private sealed record JsonRpcResult(CodexRateLimitsDto? RateLimits);

    /// <summary>Starts Codex app-server processes.</summary>
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

        /// <summary>Terminates the process and its child work.</summary>
        void Kill();
    }

    private sealed class SystemCodexProcessFactory : ICodexProcessFactory
    {
        public ICodexProcess Start(string executablePath, string arguments)
        {

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var process = new Process { StartInfo = startInfo };

            if (!process.Start())
                throw new InvalidOperationException("Codex app-server process could not be started.");

            return new SystemCodexProcess(process);
        }
    }

    private sealed class SystemCodexProcess : ICodexProcess
    {
        private readonly Process _process;

        public SystemCodexProcess(Process process)
        {

            _process = process;
        }

        public TextWriter StandardInput
            => _process.StandardInput;

        public TextReader StandardOutput
            => _process.StandardOutput;

        public bool HasExited
            => _process.HasExited;

        public void Kill()
        {

            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }

        public void Dispose()
        {

            _process.Dispose();
        }
    }
}
