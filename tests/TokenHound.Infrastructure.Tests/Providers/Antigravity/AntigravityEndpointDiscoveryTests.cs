using System.Collections.Generic;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityEndpointDiscovery"/>.
/// </summary>
public sealed class AntigravityEndpointDiscoveryTests
{
    [Fact]
    public void DiscoverEndpoint_WithValidProcessAndListeningPort_ReturnsExpectedEndpoint()
    {
        // Arrange
        const int EXPECTED_PID = 12345;
        const string EXPECTED_TOKEN = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
        const string CMD_LINE = $"\"C:\\Program Files\\Google\\language_server.exe\" --https_server_port 0 --csrf_token {EXPECTED_TOKEN} --profile default";

        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(EXPECTED_PID, CMD_LINE)],
            portResolver: pid => [54321]);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.NotNull(endpoint);
        Assert.Equal(EXPECTED_PID, endpoint.ProcessId);
        Assert.Equal(EXPECTED_TOKEN, endpoint.CsrfToken);
        Assert.Single(endpoint.CandidatePorts, 54321);
    }

    [Fact]
    public void DiscoverEndpoint_WhenNoProcesses_ReturnsNull()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [],
            portResolver: _ => [54321]);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public void DiscoverEndpoint_WhenProcessMissingCsrfTokenArg_ReturnsNull()
    {
        // Arrange
        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(1234, "\"C:\\Program Files\\Google\\language_server.exe\" --standalone")],
            portResolver: _ => [54321]);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public void DiscoverEndpoint_WhenProcessHasNoListeningPorts_ReturnsEmptyCandidatePorts()
    {
        // Arrange
        const string CMD_LINE = "language_server.exe --csrf_token 1111-2222-3333";

        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(999, CMD_LINE)],
            portResolver: _ => []);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.NotNull(endpoint);
        Assert.Equal(999, endpoint.ProcessId);
        Assert.Equal("1111-2222-3333", endpoint.CsrfToken);
        Assert.Empty(endpoint.CandidatePorts);
    }

    [Fact]
    public void DiscoverEndpoint_WithMultipleCandidatePorts_ReturnsAllPorts()
    {
        // Arrange
        const string CMD_LINE = "language_server.exe --csrf_token secret-token-xyz";

        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(888, CMD_LINE)],
            portResolver: _ => [50001, 50002]);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.NotNull(endpoint);
        Assert.Equal(2, endpoint.CandidatePorts.Count);
        Assert.Contains(50001, endpoint.CandidatePorts);
        Assert.Contains(50002, endpoint.CandidatePorts);
    }

    [Fact]
    public void DiscoverEndpoint_WhenAgyProcessRunning_ReturnsEndpointWithoutCsrfToken()
    {
        // Arrange
        const string CMD_LINE = "\"C:\\Users\\Admin\\AppData\\Local\\agy\\bin\\agy.exe\" --dangerously-skip-permissions";

        var discovery = new AntigravityEndpointDiscovery(
            processEnumerator: () => [(777, CMD_LINE)],
            portResolver: _ => [57956, 57957]);

        // Act
        var endpoint = discovery.DiscoverEndpoint();

        // Assert
        Assert.NotNull(endpoint);
        Assert.Equal(777, endpoint.ProcessId);
        Assert.Equal(string.Empty, endpoint.CsrfToken);
        Assert.Equal(2, endpoint.CandidatePorts.Count);
        Assert.Contains(57956, endpoint.CandidatePorts);
        Assert.Contains(57957, endpoint.CandidatePorts);
    }
}
