using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Infrastructure.Security;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Discovers a borrowed Copilot token in the specified strict precedence order.
/// </summary>
public sealed class CopilotCredentialDiscovery
{
    /// <summary>Non-sensitive source label for the Copilot environment variable.</summary>
    public const string ENVIRONMENT_SOURCE = "environment";

    private const string GH_SOURCE = "gh auth token";
    private const string KEYCHAIN_SOURCE = "Copilot CLI keychain";
    private const string CONFIG_SOURCE = "Copilot CLI config";
    private static readonly TimeSpan GH_COMMAND_TIMEOUT = TimeSpan.FromSeconds(3);

    private readonly ICredentialStore _credentialStore;
    private readonly CopilotCredentialTargetResolver _targetResolver;
    private readonly CopilotConfigReader _configReader;
    private readonly Func<string, string?> _environmentReader;
    private readonly Func<CancellationToken, ValueTask<string?>> _ghTokenReader;

    /// <summary>
    /// Initializes credential discovery and its read-only dependencies.
    /// </summary>
    /// <param name="credentialStore">The read-only credential store.</param>
    /// <param name="targetResolver">The validated keychain target resolver.</param>
    /// <param name="configReader">The JSONC configuration reader.</param>
    /// <param name="environmentReader">An optional environment reader for tests.</param>
    /// <param name="ghTokenReader">An optional read-only gh command reader for tests.</param>
    public CopilotCredentialDiscovery(
        ICredentialStore? credentialStore = null,
        CopilotCredentialTargetResolver? targetResolver = null,
        CopilotConfigReader? configReader = null,
        Func<string, string?>? environmentReader = null,
        Func<CancellationToken, ValueTask<string?>>? ghTokenReader = null)
    {
        _credentialStore = credentialStore ?? new WindowsCredentialManager();
        _targetResolver = targetResolver ?? new CopilotCredentialTargetResolver();
        _configReader = configReader ?? new CopilotConfigReader();
        _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
        _ghTokenReader = ghTokenReader ?? ReadGhTokenAsync;
    }

    /// <summary>
    /// Discovers the first usable borrowed token and stops probing lower sources.
    /// </summary>
    /// <param name="cancellationToken">A token to observe during discovery.</param>
    /// <returns>A non-sensitive credential descriptor, or null when none is available.</returns>
    public async ValueTask<CopilotCredential?> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var variable in new[] { "COPILOT_GITHUB_TOKEN", "GH_TOKEN", "GITHUB_TOKEN" })
        {
            var environmentToken = Usable(_environmentReader(variable));

            if (environmentToken is not null)
                return Create(environmentToken, $"{ENVIRONMENT_SOURCE}:{variable}");
        }

        var ghToken = Usable(await _ghTokenReader(cancellationToken).ConfigureAwait(false));

        if (ghToken is not null)
            return Create(ghToken, GH_SOURCE);

        var loginState = await _configReader.ReadLoginStateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var target in _targetResolver.ResolveTargets(loginState))
        {
            var keychainToken = await ReadCredentialAsync(target, cancellationToken).ConfigureAwait(false);

            if (keychainToken is not null)
                return Create(keychainToken, KEYCHAIN_SOURCE);
        }

        var configToken = Usable(await _configReader.ReadTokenAsync(cancellationToken).ConfigureAwait(false));

        return configToken is null ? null : Create(configToken, CONFIG_SOURCE);
    }

    private async ValueTask<string?> ReadCredentialAsync(
        string target,
        CancellationToken cancellationToken)
    {
        try
        {
            return Usable(await _credentialStore.ReadCredentialAsync(
                target,
                cancellationToken
            ).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or global::System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static async ValueTask<string?> ReadGhTokenAsync(CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth token",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
                return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or global::System.ComponentModel.Win32Exception)
        {
            return null;
        }

        using var timeoutCts = new CancellationTokenSource(GH_COMMAND_TIMEOUT);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);

            return process.ExitCode == 0 ? Usable(output) : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return null;
        }
        finally
        {
            if (!process.HasExited)
                TryKill(process);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (global::System.ComponentModel.Win32Exception)
        {
        }
    }

    private static CopilotCredential Create(string token, string source)
        => new() { AccessToken = token, Source = source };

    private static string? Usable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
