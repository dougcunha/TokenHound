using AwesomeAssertions;
using Serilog.Events;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Mcp;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Mcp;

/// <summary>Verifies local exposure, request limits, bind failure, and shutdown of the MCP host.</summary>
public sealed class McpServerHostTests
{
    private static readonly TimeSpan STOP_BUDGET = TimeSpan.FromSeconds(10);

    /// <summary>Verifies the endpoint binds only to IPv4 loopback and publishes both routes.</summary>
    [Fact]
    public async Task Start_BindsLoopbackEndpoints()
    {

        await using var fixture = await McpHostFixture.StartAsync(TestContext.Current.CancellationToken);

        fixture.Host.Endpoint!.Host.Should().Be("127.0.0.1");
        fixture.Host.Endpoint.AbsolutePath.Should().Be("/mcp");
        fixture.Host.SseEndpoint!.AbsolutePath.Should().Be("/mcp/sse");
        new McpServerHostOptions().Port.Should().Be(37653);
    }

    /// <summary>Verifies foreign Host and Origin values are rejected before reaching MCP.</summary>
    [Theory]
    [InlineData("localhost", null)]
    [InlineData("evil.example", null)]
    [InlineData(null, "http://evil.example")]
    [InlineData(null, "http://localhost")]
    public async Task ForeignHostOrOrigin_IsForbidden(string? hostName, string? origin)
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, fixture.Host.SseEndpoint);

        if (hostName is not null)
            request.Headers.Host = $"{hostName}:{fixture.Host.Endpoint!.Port}";

        if (origin is not null)
            request.Headers.Add("Origin", origin);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        fixture.Sink.Events.Should().Contain(static e => e.Level == LogEventLevel.Warning);
    }

    /// <summary>Verifies a matching Origin from the endpoint itself is allowed.</summary>
    [Fact]
    public async Task MatchingOrigin_OpensSseStream()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, fixture.Host.SseEndpoint);
        request.Headers.Add("Origin", $"http://127.0.0.1:{fixture.Host.Endpoint!.Port}");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
    }

    /// <summary>Verifies requests beyond the local limit receive 429 without queueing.</summary>
    [Fact]
    public async Task ExcessRequests_AreRejectedWith429()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        using var http = new HttpClient();
        var probe = new Uri(fixture.Host.Endpoint!, "/mcp/unmapped");

        for (var i = 0; i < 60; i++)
        {

            using var allowed = await http.GetAsync(probe, cancellationToken);
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var rejected = await http.GetAsync(probe, cancellationToken);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>Verifies an occupied port logs the failure and leaves the caller running.</summary>
    [Fact]
    public async Task OccupiedPort_ReturnsFalseAndLogsError()
    {

        using var occupant = new TcpListener(IPAddress.Loopback, 0);
        occupant.Start();
        var port = ((IPEndPoint)occupant.LocalEndpoint).Port;
        await using var fixture = McpHostFixture.Create(port);

        var started = await fixture.Host.StartAsync(TestContext.Current.CancellationToken);

        started.Should().BeFalse();
        fixture.Host.Endpoint.Should().BeNull();
        fixture.Sink.Events.Should().Contain(static e => e.Level == LogEventLevel.Error && e.Exception != null);
    }

    /// <summary>Verifies stop closes an active SSE session promptly and refuses new connections.</summary>
    [Fact]
    public async Task Stop_ClosesActiveSessionAndEndpoint()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await McpHostFixture.StartAsync(cancellationToken);
        await using var client = await fixture.ConnectSseAsync(cancellationToken);
        var endpoint = fixture.Host.SseEndpoint!;

        var stop = fixture.Host.StopAsync(cancellationToken);

        (await Task.WhenAny(stop, Task.Delay(STOP_BUDGET, cancellationToken))).Should().BeSameAs(stop);
        await stop;
        fixture.Host.Endpoint.Should().BeNull();
        var call = () => client.CallToolAsync(McpMetricsTools.LIST_TOOL_NAME, cancellationToken: cancellationToken).AsTask();
        await call.Should().ThrowAsync<Exception>();
        using var http = new HttpClient();
        var connect = () => http.GetAsync(endpoint, cancellationToken);
        await connect.Should().ThrowAsync<HttpRequestException>();
    }
}
