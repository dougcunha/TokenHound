using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Exposes the read-only provider metrics tools to MCP clients.</summary>
public sealed class McpMetricsTools
{
    /// <summary>The MCP name of the tool that lists every visible provider.</summary>
    public const string LIST_TOOL_NAME = "list_provider_metrics";

    /// <summary>The MCP name of the tool that looks up one provider.</summary>
    public const string GET_TOOL_NAME = "get_provider_metrics";

    private readonly McpMetricsReader _reader;

    /// <summary>Creates the tools over the shared metrics reader.</summary>
    public McpMetricsTools(McpMetricsReader reader)
    {

        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>Lists current metrics for every enabled real provider shown by TokenHound.</summary>
    [McpServerTool(
        Name = LIST_TOOL_NAME,
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Lists the latest metrics TokenHound holds for every enabled real provider. Reads cached state only; never triggers a provider refresh.")]
    public McpMetricsListResult ListProviderMetrics()
        => _reader.ListMetrics();

    /// <summary>Gets current metrics for one provider, or why they are unavailable.</summary>
    [McpServerTool(
        Name = GET_TOOL_NAME,
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Gets the latest metrics for one provider. lookupState is available, unknown, disabled, pending, or synthetic; only available carries metrics.")]
    public McpMetricsLookupResult GetProviderMetrics(
        [Description("Stable provider identifier, such as claude or copilot. Case-insensitive.")] string providerId)
    {

        if (string.IsNullOrWhiteSpace(providerId))
            throw new McpException($"{nameof(providerId)} must not be blank.");

        return _reader.GetMetrics(providerId);
    }
}
