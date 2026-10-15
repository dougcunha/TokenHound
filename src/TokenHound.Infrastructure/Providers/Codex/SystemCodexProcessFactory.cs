using TokenHound.Infrastructure.Processes;

namespace TokenHound.Infrastructure.Providers.Codex;

internal sealed class SystemCodexProcessFactory : CodexAppServerClient.ICodexProcessFactory
{
    /// <inheritdoc />
    public CodexAppServerClient.ICodexProcess Start(string executablePath, string arguments)
        => new SystemCodexProcess(OwnedProcess.Start(executablePath, arguments));
}
