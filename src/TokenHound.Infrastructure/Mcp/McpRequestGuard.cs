using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Rejects requests whose Host or Origin does not name the loopback endpoint.</summary>
internal sealed class McpRequestGuard
{
    private readonly ILogger _logger;

    /// <summary>Creates a guard that logs rejected requests.</summary>
    public McpRequestGuard(ILogger logger)
    {

        _logger = logger;
    }

    /// <summary>Runs the next middleware only for an exact local Host and an absent or matching Origin.</summary>
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {

        var authority = $"{McpServerHost.LOOPBACK_HOST}:{context.Connection.LocalPort}";
        var host = context.Request.Host.Value;
        var origin = context.Request.Headers.Origin.ToString();

        if (IsAllowed(authority, host, origin))
            return next(context);

        _logger.Warning(
            "Rejected MCP request with host {Host} and origin {Origin}",
            host,
            origin
        );
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        return Task.CompletedTask;
    }

    private static bool IsAllowed(string authority, string? host, string origin)
    {

        if (!string.Equals(host, authority, StringComparison.OrdinalIgnoreCase))
            return false;

        return origin.Length == 0
            || string.Equals(origin, $"http://{authority}", StringComparison.OrdinalIgnoreCase);
    }
}
