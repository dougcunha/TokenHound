namespace TokenHound.Infrastructure.Mcp;

/// <summary>Configures the local MCP metrics endpoint.</summary>
public sealed record McpServerHostOptions
{
    /// <summary>The stable loopback port documented for MCP clients.</summary>
    public const int DEFAULT_PORT = 37653;

    /// <summary>Gets the loopback port to bind; zero selects an ephemeral port.</summary>
    public int Port { get; init; } = DEFAULT_PORT;
}
