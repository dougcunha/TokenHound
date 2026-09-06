using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Verifies Codex app-server discovery, JSON-RPC exchange, parsing, and watchdog behavior.</summary>
public sealed class CodexAppServerClientTests
{
    private const string RATE_LIMITS_RESPONSE = "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"rateLimits\":{\"planType\":\"plus\",\"rateLimitReachedType\":\"rate_limit_reached\",\"primary\":{\"usedPercent\":78.0,\"windowDurationMins\":300,\"resetsAt\":1788659888},\"secondary\":{\"usedPercent\":12.0,\"windowDurationMins\":10080,\"resetsAt\":1789246688}}}}";

    /// <summary>Verifies the handshake messages and rate-limit response parsing.</summary>
    [Fact]
    public async Task GetRateLimitsAsync_WithSuccessfulServer_ReturnsWindowsAndSendsHandshake()
    {

        var process = new FakeProcess(new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}\n{\"jsonrpc\":\"2.0\",\"method\":\"initialized\"}\n" + RATE_LIMITS_RESPONSE));
        var factory = new FakeProcessFactory(process);
        var client = new CodexAppServerClient(factory, () => "codex.exe");

        var limits = await client.GetRateLimitsAsync(TestContext.Current.CancellationToken);

        limits.Should().NotBeNull();
        limits!.PlanType.Should().Be("plus");
        limits.RateLimitReachedType.Should().Be("rate_limit_reached");
        limits.Primary.Should().NotBeNull();
        limits.Primary!.UsedPercent.Should().Be(78.0);
        limits.Primary.WindowDurationMins.Should().Be(300);
        limits.Primary.ResetsAt.Should().Be(1788659888);
        limits.Secondary!.UsedPercent.Should().Be(12.0);
        factory.ExecutablePath.Should().Be("codex.exe");
        factory.Arguments.Should().Be("app-server");
        process.Input.ToString().Should().Contain("\"method\":\"initialize\"");
        process.Input.ToString().Should().Contain("\"method\":\"initialized\"");
        process.Input.ToString().Should().Contain("\"method\":\"account/rateLimits/read\"");
        process.KillCalled.Should().BeTrue();
        process.Disposed.Should().BeTrue();
    }

    /// <summary>Verifies that missing executable discovery returns null without starting a process.</summary>
    [Fact]
    public async Task GetRateLimitsAsync_WhenExecutableIsMissing_ReturnsNull()
    {

        var factory = new FakeProcessFactory(new FakeProcess(new StringReader(string.Empty)));
        var client = new CodexAppServerClient(factory, static () => null);

        var limits = await client.GetRateLimitsAsync(TestContext.Current.CancellationToken);

        limits.Should().BeNull();
        factory.StartCalled.Should().BeFalse();
    }

    /// <summary>Verifies that standard ChatGPT installation locations are searched before PATH.</summary>
    [Fact]
    public void FindExecutable_WhenInstalledInLocalAppData_ReturnsCodexPath()
    {

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"codex_executable_{Guid.NewGuid():N}");
        var expectedPath = Path.Combine(tempDirectory, Path.Combine("Programs", "ChatGPT", "resources"), "codex.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);

        try
        {

            File.WriteAllText(expectedPath, string.Empty);

            var result = CodexAppServerClient.FindExecutable(
                localAppDataDirectory: tempDirectory,
                programFilesDirectory: string.Empty,
                userProfileDirectory: string.Empty,
                pathEnvironment: string.Empty
            );

            result.Should().Be(expectedPath);
        }
        finally
        {

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies that an unresponsive app server is killed at the configured timeout.</summary>
    [Fact]
    public async Task GetRateLimitsAsync_WhenServerTimesOut_KillsProcessAndReturnsNull()
    {

        var process = new FakeProcess(new BlockingTextReader());
        var factory = new FakeProcessFactory(process);
        var client = new CodexAppServerClient(factory, () => "codex.exe", TimeSpan.FromMilliseconds(50));

        var limits = await client.GetRateLimitsAsync(TestContext.Current.CancellationToken);

        limits.Should().BeNull();
        process.KillCalled.Should().BeTrue();
        process.Disposed.Should().BeTrue();
    }

    private sealed class FakeProcessFactory : CodexAppServerClient.ICodexProcessFactory
    {
        private readonly CodexAppServerClient.ICodexProcess _process;

        public FakeProcessFactory(CodexAppServerClient.ICodexProcess process)
        {

            _process = process;
        }

        public string? ExecutablePath { get; private set; }

        public string? Arguments { get; private set; }

        public bool StartCalled { get; private set; }

        public CodexAppServerClient.ICodexProcess Start(string executablePath, string arguments)
        {

            StartCalled = true;
            ExecutablePath = executablePath;
            Arguments = arguments;

            return _process;
        }
    }

    private sealed class FakeProcess : CodexAppServerClient.ICodexProcess
    {
        public FakeProcess(TextReader standardOutput)
        {

            StandardOutput = standardOutput;
        }

        public StringWriter Input { get; } = new();

        public TextWriter StandardInput
            => Input;

        public TextReader StandardOutput { get; }

        public bool HasExited { get; private set; }

        public bool KillCalled { get; private set; }

        public bool Disposed { get; private set; }

        public void Kill()
        {

            KillCalled = true;
            HasExited = true;
        }

        public void Dispose()
        {

            Disposed = true;
        }
    }

    private sealed class BlockingTextReader : TextReader
    {
        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);

            return null;
        }
    }
}
