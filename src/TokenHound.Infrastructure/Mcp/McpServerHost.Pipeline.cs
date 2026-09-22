using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using System;
using System.Net;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Builds the Kestrel, security, rate-limit, and MCP transport pipeline.</summary>
public sealed partial class McpServerHost
{
    private const string RATE_LIMIT_PARTITION = "mcp";
    private const int REQUESTS_PER_WINDOW = 60;
    private const int MAX_CONNECTIONS = 16;
    private static readonly TimeSpan RATE_LIMIT_WINDOW = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SHUTDOWN_TIMEOUT = TimeSpan.FromSeconds(5);

    private WebApplication BuildApplication()
    {

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(ConfigureKestrel);
        builder.Services.Configure<HostOptions>(static options => options.ShutdownTimeout = SHUTDOWN_TIMEOUT);
        builder.Services.AddRouting();
        builder.Services.AddRateLimiter(ConfigureRateLimiter);
        builder.Services.AddMcpServer()
            .WithHttpTransport(ConfigureTransport)
            .WithTools(_tools);

        var app = builder.Build();
        app.Use(new McpRequestGuard(_logger).InvokeAsync);
        app.UseRateLimiter();
        app.MapMcp(ROUTE);

        return app;
    }

    private void ConfigureKestrel(KestrelServerOptions options)
    {

        options.Listen(IPAddress.Loopback, _options.Port);
        options.Limits.MaxConcurrentConnections = MAX_CONNECTIONS;
    }

    private void ConfigureRateLimiter(RateLimiterOptions options)
    {

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(static _ => CreatePartition());
        options.OnRejected = OnRateLimited;
    }

    private static RateLimitPartition<string> CreatePartition()
    {

        return RateLimitPartition.GetFixedWindowLimiter(
            RATE_LIMIT_PARTITION,
            static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = REQUESTS_PER_WINDOW,
                Window = RATE_LIMIT_WINDOW,
                QueueLimit = 0
            });
    }

    private ValueTask OnRateLimited(OnRejectedContext context, CancellationToken cancellationToken)
    {

        _logger.Warning("Rejected MCP request {Path}: local request limit exceeded", context.HttpContext.Request.Path.Value);

        return ValueTask.CompletedTask;
    }

    private static void ConfigureTransport(HttpServerTransportOptions options)
    {

        options.SessionMode = HttpServerSessionMode.Stateful;
#pragma warning disable MCP9004 // DEC-11: legacy SSE is an approved PRD requirement; the local rate limit compensates for its missing backpressure (DEC-09).
        options.EnableLegacySse = true;
#pragma warning restore MCP9004
    }
}
