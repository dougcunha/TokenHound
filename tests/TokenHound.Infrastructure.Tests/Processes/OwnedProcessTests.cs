using AwesomeAssertions;
using System.Diagnostics;
using TokenHound.Infrastructure.Processes;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Processes;

/// <summary>Verifies CLI containment using real Windows helper processes.</summary>
public sealed class OwnedProcessTests
{
    private static string Helper
        => Path.Combine(AppContext.BaseDirectory, "ProcessHelper", "TokenHound.ProcessTestHelper.exe");

    /// <summary>EOF lets a cooperative CLI finish before forced cleanup.</summary>
    [Fact]
    public async Task DisposeAsync_ClosesStdinAndAllowsGracefulExit()
    {

        var marker = Path.Combine(Path.GetTempPath(), $"tokenhound-eof-{Guid.NewGuid():N}");

        try
        {

            await using var process = OwnedProcess.Start(Helper, $"eof \"{marker}\"");
            (await ReadLineAsync(process)).Should().Be("ready");
            await process.DisposeAsync();
            File.ReadAllText(marker).Should().Be("graceful");
        }
        finally
        {

            File.Delete(marker);
        }
    }

    /// <summary>Continuous stderr draining prevents pipe saturation.</summary>
    [Fact]
    public async Task StderrBeyondPipeCapacity_DoesNotBlockOutput()
    {

        await using var process = OwnedProcess.Start(Helper, "stderr");

        (await ReadLineAsync(process)).Should().Be("ready");
    }

    /// <summary>A non-cooperative root and its child terminate after the grace period.</summary>
    [Fact]
    public async Task DisposeAsync_KillsTreeWhenRootIgnoresEof()
    {

        await using var process = OwnedProcess.Start(Helper, "tree");
        using var root = Process.GetProcessById(process.Id);
        using var child = Process.GetProcessById(int.Parse((await ReadLineAsync(process))!));

        await process.DisposeAsync();
        await AssertExitedAsync(root);
        await AssertExitedAsync(child);
    }

    /// <summary>Closing the job also removes descendants of an already exited root.</summary>
    [Fact]
    public async Task DisposeAsync_KillsDescendantAfterRootAlreadyExited()
    {

        await using var process = OwnedProcess.Start(Helper, "orphan");
        using var child = Process.GetProcessById(int.Parse((await ReadLineAsync(process))!));
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        await process.DisposeAsync();
        await AssertExitedAsync(child);
    }

    /// <summary>OS cleanup closes the job when only the owner is killed.</summary>
    [Fact]
    public async Task KillingOwnerWithoutTreeCleanup_ClosesJobAndKillsDescendants()
    {

        using var owner = Process.Start(new ProcessStartInfo(Helper, "owner")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!;

        try
        {

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var ids = (await owner.StandardOutput.ReadLineAsync(timeout.Token))!.Split(',');
            using var root = Process.GetProcessById(int.Parse(ids[0]));
            using var child = Process.GetProcessById(int.Parse(ids[1]));
            // Kill only the owner to simulate Task Manager/crash without managed cleanup.
            owner.Kill(entireProcessTree: false);
            await AssertExitedAsync(owner);
            await AssertExitedAsync(root);
            await AssertExitedAsync(child);
        }
        finally
        {

            if (!owner.HasExited)
                owner.Kill(entireProcessTree: true);
        }
    }

    private static async Task<string?> ReadLineAsync(OwnedProcess process)
    {

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        return await process.StandardOutput.ReadLineAsync(timeout.Token).AsTask().WaitAsync(timeout.Token);
    }

    /// <summary>Twenty real pipe exchanges share one process.</summary>
    [Fact]
    public async Task RealRpcProcess_IsReusedForMultipleQueries()
    {

        using var factory = new RpcFactory("rpc");
        await using var client = new CodexAppServerClient(factory, static () => Helper);

        for (var query = 0; query < 20; query++)
            (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken))!.Primary!.UsedPercent.Should().Be(42);

        factory.Starts.Should().Be(1);
    }

    /// <summary>A real pipe read observes the watchdog and allows cleanup.</summary>
    [Fact]
    public async Task RealRpcPipe_TimeoutRetiresServerWithoutHangingCleanup()
    {

        using var factory = new RpcFactory("rpc-hang");
        await using var client = new CodexAppServerClient(factory, static () => Helper, TimeSpan.FromSeconds(1));
        var reading = client.GetRateLimitsAsync(TestContext.Current.CancellationToken);

        (await reading.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).Should().BeNull();
        factory.Process!.HasExited.Should().BeTrue();
    }

    private sealed class RpcFactory(string mode) : CodexAppServerClient.ICodexProcessFactory, IDisposable
    {
        internal int Starts { get; private set; }
        internal Process? Process { get; private set; }

        /// <inheritdoc />
        public CodexAppServerClient.ICodexProcess Start(string executablePath, string arguments)
        {

            Starts++;
            var process = OwnedProcess.Start(executablePath, mode);
            Process = global::System.Diagnostics.Process.GetProcessById(process.Id);

            return new SystemCodexProcess(process);
        }

        /// <inheritdoc />
        public void Dispose()
            => Process?.Dispose();
    }

    private static async Task AssertExitedAsync(Process process)
    {

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await process.WaitForExitAsync(timeout.Token);
        process.HasExited.Should().BeTrue();
    }
}
