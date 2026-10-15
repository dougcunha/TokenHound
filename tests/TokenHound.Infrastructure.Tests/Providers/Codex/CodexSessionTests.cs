using AwesomeAssertions;
using System.Text.Json;
using System.Threading.Channels;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Exercises persistent Codex protocol ownership and recovery.</summary>
public sealed class CodexSessionTests
{
    /// <summary>Successful polls retain the server and initialize it once.</summary>
    [Fact]
    public async Task SequentialReads_ReuseProcessAndInitializeOnce()
    {

        var process = new SessionProcess();
        var factory = new Factory(process);
        await using var client = CreateClient(factory);

        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken))!.Primary!.UsedPercent.Should().Be(2);
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken))!.Primary!.UsedPercent.Should().Be(3);

        factory.Starts.Should().Be(1);
        process.Initializations.Should().Be(1);
        process.Killed.Should().BeFalse();
        await client.DisposeAsync();
        process.Killed.Should().BeTrue();
    }

    /// <summary>Concurrent polls cannot interleave protocol requests.</summary>
    [Fact]
    public async Task ConcurrentReads_SerializeProtocolRequests()
    {

        var process = new SessionProcess { BlockLimits = true };
        await using var client = CreateClient(new Factory(process));
        var first = client.GetRateLimitsAsync(TestContext.Current.CancellationToken);
        await process.Requested.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = client.GetRateLimitsAsync(TestContext.Current.CancellationToken);

        process.Requests.Should().Be(1);
        process.BlockLimits = false;
        process.Respond(2);

        (await first)!.Primary!.UsedPercent.Should().Be(2);
        (await second)!.Primary!.UsedPercent.Should().Be(3);
    }

    /// <summary>Cancellation while queued leaves the active request intact.</summary>
    [Fact]
    public async Task CancelledWaiter_DoesNotRetireActiveSession()
    {

        var process = new SessionProcess { BlockLimits = true };
        await using var client = CreateClient(new Factory(process));
        var first = client.GetRateLimitsAsync(TestContext.Current.CancellationToken);
        await process.Requested.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var waiting = client.GetRateLimitsAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        process.Killed.Should().BeFalse();
        process.Respond(2);
        (await first).Should().NotBeNull();
    }

    /// <summary>Cancellation during a request retires its session.</summary>
    [Fact]
    public async Task CallerCancellation_RetiresSessionAndPropagates()
    {

        var process = new SessionProcess { BlockLimits = true };
        await using var client = CreateClient(new Factory(process));
        using var cancellation = new CancellationTokenSource();
        var reading = client.GetRateLimitsAsync(cancellation.Token);
        await process.Requested.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading);
        process.Killed.Should().BeTrue();
    }

    /// <summary>Disposal cancels outstanding I/O and prevents new queries.</summary>
    [Fact]
    public async Task Disposal_CancelsInflightReadAndRejectsNewQueries()
    {

        var process = new SessionProcess { BlockLimits = true };
        var client = CreateClient(new Factory(process));
        var reading = client.GetRateLimitsAsync(TestContext.Current.CancellationToken);
        await process.Requested.Task.WaitAsync(TestContext.Current.CancellationToken);
        await client.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetRateLimitsAsync(TestContext.Current.CancellationToken));
        process.Killed.Should().BeTrue();
    }

    /// <summary>Repeated failures increase the delay before the next start.</summary>
    [Fact]
    public async Task FailedStartup_UsesExponentialBackoffBeforeRecovery()
    {

        var clock = new Clock();
        var factory = new Factory(new SessionProcess()) { FailuresRemaining = 2 };
        await using var client = new CodexAppServerClient(factory, static () => "codex.exe", timeProvider: clock);

        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        factory.Starts.Should().Be(1);
        clock.Advance(TimeSpan.FromSeconds(5));
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        clock.Advance(TimeSpan.FromSeconds(9));
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        factory.Starts.Should().Be(2);
        clock.Advance(TimeSpan.FromSeconds(1));
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().NotBeNull();
        factory.Starts.Should().Be(3);
    }

    /// <summary>A dead server is retired before a delayed restart.</summary>
    [Fact]
    public async Task ExitedServer_IsRetiredAndRestartedAfterBackoff()
    {

        var clock = new Clock();
        var first = new SessionProcess();
        var second = new SessionProcess();
        var factory = new Factory(first, second);
        await using var client = new CodexAppServerClient(factory, static () => "codex.exe", timeProvider: clock);
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().NotBeNull();
        first.Kill();

        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().BeNull();
        factory.Starts.Should().Be(1);
        clock.Advance(TimeSpan.FromSeconds(5));
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken)).Should().NotBeNull();
        second.Initializations.Should().Be(1);
        factory.Starts.Should().Be(2);
    }

    /// <summary>Only the current numeric response ID can satisfy a request.</summary>
    [Fact]
    public async Task NotificationsMalformedAndStaleResponses_DoNotReplaceCurrentReply()
    {

        var process = new SessionProcess { InjectNoise = true };
        await using var client = CreateClient(new Factory(process));

        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken))!.Primary!.UsedPercent.Should().Be(2);
        (await client.GetRateLimitsAsync(TestContext.Current.CancellationToken))!.Primary!.UsedPercent.Should().Be(3);
    }

    private static CodexAppServerClient CreateClient(Factory factory)
        => new(factory, static () => "codex.exe");

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow()
            => _now;

        internal void Advance(TimeSpan amount)
            => _now += amount;
    }

    private sealed class Factory(params SessionProcess[] processes) : CodexAppServerClient.ICodexProcessFactory
    {
        private readonly Queue<SessionProcess> _processes = new(processes);
        internal int Starts { get; private set; }
        internal int FailuresRemaining { get; set; }

        /// <inheritdoc />
        public CodexAppServerClient.ICodexProcess Start(string executablePath, string arguments)
        {

            Starts++;

            if (FailuresRemaining-- > 0)
                throw new IOException("Simulated startup failure");

            return _processes.Dequeue();
        }
    }

    private sealed class SessionProcess : TextWriter, CodexAppServerClient.ICodexProcess
    {
        private readonly Channel<string> _output = Channel.CreateUnbounded<string>();
        internal TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool BlockLimits { get; set; }
        internal bool InjectNoise { get; init; }
        internal bool Killed { get; private set; }
        internal int Initializations { get; private set; }
        internal int Requests { get; private set; }
        /// <inheritdoc />
        public override global::System.Text.Encoding Encoding
            => global::System.Text.Encoding.UTF8;

        /// <inheritdoc />
        public TextWriter StandardInput
            => this;

        /// <inheritdoc />
        public TextReader StandardOutput
            => new ChannelReader(_output.Reader);

        /// <inheritdoc />
        public bool HasExited
            => Killed;

        /// <inheritdoc />
        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {

            using var message = JsonDocument.Parse(buffer.ToString());

            if (!message.RootElement.TryGetProperty("id", out var id))
                return Task.CompletedTask;

            if (id.GetInt32() == 1)
            {

                Initializations++;
                _output.Writer.TryWrite("{\"id\":1,\"result\":{}}");
            }
            else
                Request(id.GetInt32());

            return Task.CompletedTask;
        }

        private void Request(int id)
        {

            Requests++;
            Requested.TrySetResult();

            if (InjectNoise)
            {

                _output.Writer.TryWrite("not json");
                _output.Writer.TryWrite("{\"method\":\"account/rateLimits/updated\"}");
                _output.Writer.TryWrite("{\"id\":-1,\"result\":{\"rateLimits\":{\"primary\":{\"usedPercent\":99}}}}");
            }

            if (!BlockLimits)
                Respond(id);
        }

        internal void Respond(int id)
            => _output.Writer.TryWrite($"{{\"id\":{id},\"result\":{{\"rateLimits\":{{\"primary\":{{\"usedPercent\":{id}}}}}}}}}");

        /// <inheritdoc />
        public void Kill()
            => Killed = true;
    }

    private sealed class ChannelReader(ChannelReader<string> reader) : TextReader
    {
        /// <inheritdoc />
        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
            => await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }
}
