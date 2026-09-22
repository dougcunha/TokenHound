using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Mcp;

namespace TokenHound.App;

/// <summary>Starts the local MCP metrics endpoint alongside the HUD.</summary>
public partial class App
{
    private void StartMcpServer(UsageStore? usageStore)
    {

        if (usageStore is null || _lifetime is null)
            return;

        var host = new McpServerHost(new McpMetricsReader(usageStore));
        var lifetimeToken = _lifetime.LifetimeToken;
        _lifetime.TrackAsyncResource(host);
        _lifetime.TrackStartupTask(Task.Run(() => StartMcpServerAsync(host, lifetimeToken), lifetimeToken));
    }

    private static async Task StartMcpServerAsync(McpServerHost host, CancellationToken cancellationToken)
    {

        try
        {

            if (!await host.StartAsync(cancellationToken))
                Log.Warning("MCP metrics endpoint is unavailable for this run; the HUD continues without it");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            Log.Error(ex, "MCP metrics endpoint failed to start; the HUD continues without it");
        }
    }
}
