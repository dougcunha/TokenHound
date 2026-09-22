using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Mcp;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Mcp;

/// <summary>Verifies the metrics tools through a real MCP client over legacy SSE.</summary>
public sealed class McpTransportTests
{
    /// <summary>Verifies discovery of exactly the two read-only tools and their structured results.</summary>
    [Fact]
    public async Task Sse_ListsBothToolsAndReturnsStructuredMetrics()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        var provider = fixture.Register(McpHostFixture.CreateSnapshot("claude", 70));
        await fixture.Store.RefreshNowAsync(cancellationToken);
        await using var client = await fixture.ConnectSseAsync(cancellationToken);

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        var list = await client.CallToolAsync(McpMetricsTools.LIST_TOOL_NAME, cancellationToken: cancellationToken);
        var found = await CallGetAsync(client, "CLAUDE", cancellationToken);
        var missing = await CallGetAsync(client, "missing", cancellationToken);

        tools.Select(static tool => tool.Name).Should().BeEquivalentTo(McpMetricsTools.LIST_TOOL_NAME, McpMetricsTools.GET_TOOL_NAME);
        var providers = McpHostFixture.ReadStructured(list).GetProperty("providers");
        providers.GetArrayLength().Should().Be(1);
        providers[0].GetProperty("providerId").GetString().Should().Be("claude");
        providers[0].GetProperty("limitWindows")[0].GetProperty("remainingUnits").GetInt64().Should().Be(70);
        McpHostFixture.ReadStructured(found).GetProperty("lookupState").GetString().Should().Be("available");
        McpHostFixture.ReadStructured(missing).GetProperty("lookupState").GetString().Should().Be("unknown");
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies the next call sees a store update without MCP dispatching provider work.</summary>
    [Fact]
    public async Task Sse_NextCallSeesStoreUpdateWithoutDispatch()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        var provider = fixture.Register(McpHostFixture.CreateSnapshot("claude", 70), McpHostFixture.CreateSnapshot("claude", 40));
        await fixture.Store.RefreshNowAsync(cancellationToken);
        await using var client = await fixture.ConnectSseAsync(cancellationToken);

        var before = await ReadRemainingAsync(client, cancellationToken);
        await ReadRemainingAsync(client, cancellationToken);
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
        await fixture.Store.RefreshNowAsync(cancellationToken);
        var after = await ReadRemainingAsync(client, cancellationToken);

        before.Should().Be(70);
        after.Should().Be(40);
        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies a blank provider ID is a tool error, not a crash.</summary>
    [Fact]
    public async Task Sse_BlankProviderId_ReturnsToolError()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        await using var client = await fixture.ConnectSseAsync(cancellationToken);

        var result = await CallGetAsync(client, " ", cancellationToken);

        result.IsError.Should().BeTrue();
    }

    /// <summary>Verifies concurrent reads while the store refreshes return whole results.</summary>
    [Fact]
    public async Task Sse_ConcurrentReadsDuringRefresh_Succeed()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        fixture.Register(McpHostFixture.CreateSnapshot("claude", 70), McpHostFixture.CreateSnapshot("claude", 40));
        await fixture.Store.RefreshNowAsync(cancellationToken);
        await using var client = await fixture.ConnectSseAsync(cancellationToken);

        var reads = Enumerable.Range(0, 10).Select(_ => ReadRemainingAsync(client, cancellationToken)).ToList();
        var refreshes = Enumerable.Range(0, 5).Select(_ => fixture.Store.RefreshNowAsync(cancellationToken));
        await Task.WhenAll(refreshes);
        var values = await Task.WhenAll(reads);

        values.Should().OnlyContain(static value => value == 70 || value == 40);
    }

    private static Task<CallToolResult> CallGetAsync(
        McpClient client,
        string providerId,
        CancellationToken cancellationToken)
    {

        var arguments = new Dictionary<string, object?> { ["providerId"] = providerId };

        return client.CallToolAsync(McpMetricsTools.GET_TOOL_NAME, arguments, cancellationToken: cancellationToken).AsTask();
    }

    private static async Task<long> ReadRemainingAsync(McpClient client, CancellationToken cancellationToken)
    {

        var result = await CallGetAsync(client, "claude", cancellationToken);

        return McpHostFixture.ReadStructured(result)
            .GetProperty("provider")
            .GetProperty("limitWindows")[0]
            .GetProperty("remainingUnits")
            .GetInt64();
    }
}
