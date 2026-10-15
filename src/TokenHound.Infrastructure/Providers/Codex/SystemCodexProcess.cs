using TokenHound.Infrastructure.Processes;

namespace TokenHound.Infrastructure.Providers.Codex;

internal sealed class SystemCodexProcess(OwnedProcess process) : CodexAppServerClient.ICodexProcess
{
    /// <inheritdoc />
    public TextWriter StandardInput
        => process.StandardInput;

    /// <inheritdoc />
    public TextReader StandardOutput
        => process.StandardOutput;

    /// <inheritdoc />
    public bool HasExited
        => process.HasExited;

    /// <inheritdoc />
    public void Kill()
        => process.Kill();

    /// <inheritdoc />
    public void Dispose()
        => process.Dispose();

    /// <inheritdoc />
    public ValueTask StopAsync()
        => process.DisposeAsync();
}
