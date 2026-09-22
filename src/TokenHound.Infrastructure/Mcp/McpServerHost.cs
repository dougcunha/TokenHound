using Microsoft.AspNetCore.Builder;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Hosts the read-only metrics tools on a loopback MCP endpoint with Streamable HTTP and legacy SSE.</summary>
public sealed partial class McpServerHost : IAsyncDisposable
{
    internal const string LOOPBACK_HOST = "127.0.0.1";
    private const string ROUTE = "/mcp";

    private readonly McpMetricsTools _tools;
    private readonly McpServerHostOptions _options;
    private readonly ILogger _logger;
    private WebApplication? _app;
    private int _boundPort;

    /// <summary>Creates a stopped host over the shared metrics reader.</summary>
    public McpServerHost(McpMetricsReader reader, McpServerHostOptions? options = null, ILogger? logger = null)
    {

        _tools = new McpMetricsTools(reader);
        _options = options ?? new McpServerHostOptions();
        _logger = logger ?? Log.ForContext<McpServerHost>();
    }

    /// <summary>Gets the Streamable HTTP endpoint while running, otherwise <see langword="null"/>.</summary>
    public Uri? Endpoint
        => _app is null ? null : new Uri($"http://{LOOPBACK_HOST}:{_boundPort}{ROUTE}");

    /// <summary>Gets the legacy SSE endpoint while running, otherwise <see langword="null"/>.</summary>
    public Uri? SseEndpoint
        => _app is null ? null : new Uri($"http://{LOOPBACK_HOST}:{_boundPort}{ROUTE}/sse");

    /// <summary>Starts listening; returns <see langword="false"/> after logging when the port cannot be bound.</summary>
    public async Task<bool> StartAsync(CancellationToken cancellationToken)
    {

        if (_app is not null)
            throw new InvalidOperationException("The MCP endpoint is already running.");

        var app = BuildApplication();

        try
        {

            await app.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {

            _logger.Error(
                ex,
                "MCP endpoint {Endpoint} failed to start: {ExceptionType}",
                $"http://{LOOPBACK_HOST}:{_options.Port}{ROUTE}",
                ex.GetType().Name
            );
            await app.DisposeAsync().ConfigureAwait(false);

            return false;
        }
        catch (OperationCanceledException)
        {

            await app.DisposeAsync().ConfigureAwait(false);

            throw;
        }

        _boundPort = new Uri(app.Urls.First()).Port;
        _app = app;
        _logger.Information("MCP endpoint listening at {Endpoint} with legacy SSE at {SseEndpoint}", Endpoint, SseEndpoint);

        return true;
    }

    /// <summary>Stops accepting calls and closes active MCP sessions.</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {

        var app = Interlocked.Exchange(ref _app, null);

        if (app is null)
            return;

        try
        {

            await app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            await app.DisposeAsync().ConfigureAwait(false);
        }

        _logger.Information("MCP endpoint stopped");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => new(StopAsync(CancellationToken.None));
}
