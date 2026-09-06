using AwesomeAssertions;
using NSubstitute;

namespace TokenHound.Infrastructure.Tests;

/// <summary>
/// Verifies testing stack integration with xUnit.net v3, AwesomeAssertions, and NSubstitute.
/// </summary>
public sealed class InfrastructureSmokeTests
{
    /// <summary>
    /// Contract used to verify test isolation and mocking integration.
    /// </summary>
    public interface IProbeService
    {
        /// <summary>
        /// Gets sample probe data.
        /// </summary>
        string GetProbeData(string id);
    }

    /// <summary>
    /// Validates assertions and mocking behavior within the Infrastructure test project.
    /// </summary>
    [Fact]
    public void SmokeTest_VerifiesInfrastructureTestStack()
    {
        var probe = Substitute.For<IProbeService>();
        probe.GetProbeData("id-2").Returns("ready");

        var result = probe.GetProbeData("id-2");

        result.Should().Be("ready");
        probe.Received(1).GetProbeData("id-2");
    }
}
