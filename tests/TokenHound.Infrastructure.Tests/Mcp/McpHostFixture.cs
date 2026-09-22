using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Mcp;

namespace TokenHound.Infrastructure.Tests.Mcp;

/// <summary>Runs a real MCP host over an in-memory store on an ephemeral loopback port.</summary>
internal sealed class McpHostFixture : IAsyncDisposable
{
    private static readonly DateTimeOffset SAMPLE_TIME = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    private McpHostFixture(int port)
    {

        Logger = new LoggerConfiguration().WriteTo.Sink(Sink).CreateLogger();
        Host = new McpServerHost(new McpMetricsReader(Store), new McpServerHostOptions { Port = port }, Logger);
    }

    /// <summary>Gets the store the host reads.</summary>
    public UsageStore Store { get; } = new();

    /// <summary>Gets the host under test.</summary>
    public McpServerHost Host { get; }

    /// <summary>Gets the events the host logged.</summary>
    public TestLogSink Sink { get; } = new();

    private Logger Logger { get; }

    /// <summary>Creates a stopped host bound to the given port, zero for ephemeral.</summary>
    public static McpHostFixture Create(int port = 0)
        => new(port);

    /// <summary>Creates and starts a host on an ephemeral port.</summary>
    public static async Task<McpHostFixture> StartAsync(CancellationToken cancellationToken)
    {

        var fixture = Create();
        var started = await fixture.Host.StartAsync(cancellationToken);

        if (started)
            return fixture;

        await fixture.DisposeAsync();

        throw new InvalidOperationException("The MCP test host failed to start.");
    }

    /// <summary>Registers a provider double that returns the snapshots in order, repeating the last.</summary>
    public IUsageProvider Register(params Snapshot[] snapshots)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns(snapshots[0].ProviderId);
        var callIndex = -1;
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(snapshots[Math.Min(Interlocked.Increment(ref callIndex), snapshots.Length - 1)]));
        Store.RegisterProvider(provider);

        return provider;
    }

    /// <summary>Connects a real MCP client through the legacy SSE transport.</summary>
    public Task<McpClient> ConnectSseAsync(CancellationToken cancellationToken)
    {

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = Host.SseEndpoint!,
            TransportMode = HttpTransportMode.Sse
        });

        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    /// <summary>Creates a successful snapshot with one known-denominator window.</summary>
    public static Snapshot CreateSnapshot(string providerId, long remainingUnits)
    {

        return new Snapshot
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = SAMPLE_TIME,
            LimitWindows = [new LimitWindow { Name = "session", RemainingUnits = remainingUnits, TotalUnits = 100 }]
        };
    }

    /// <summary>Parses a tool call's structured content.</summary>
    public static JsonElement ReadStructured(CallToolResult result)
        => JsonDocument.Parse(JsonSerializer.Serialize(result.StructuredContent)).RootElement;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {

        await Host.DisposeAsync();
        Store.Dispose();
        await Logger.DisposeAsync();
    }

    /// <summary>Collects log events in memory.</summary>
    public sealed class TestLogSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        /// <summary>Gets a copy of the collected events.</summary>
        public IReadOnlyList<LogEvent> Events
        {
            get
            {

                lock (_events)
                    return [.. _events];
            }
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {

            lock (_events)
                _events.Add(logEvent);
        }
    }
}
