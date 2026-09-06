using AwesomeAssertions;
using NSubstitute;

namespace TokenHound.Core.Tests;

/// <summary>
/// Verifies testing stack integration with xUnit.net v3, AwesomeAssertions, and NSubstitute.
/// </summary>
public sealed class CoreSmokeTests
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
    /// Validates assertions and mocking behavior within the Core test project.
    /// </summary>
    [Fact]
    public void SmokeTest_VerifiesCoreTestStack()
    {
        var probe = Substitute.For<IProbeService>();
        probe.GetProbeData("id-1").Returns("ok");

        var result = probe.GetProbeData("id-1");

        result.Should().Be("ok");
        probe.Received(1).GetProbeData("id-1");
    }
}
