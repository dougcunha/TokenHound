using AwesomeAssertions;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies decoding of the Windows "Startup apps" approval value in <see cref="StartupApprovalPolicy"/> (TC-01).
/// </summary>
public sealed class StartupApprovalPolicyTests
{
    /// <summary>
    /// Verifies that only a non-empty value whose first byte is odd counts as disabled.
    /// </summary>
    /// <param name="value">The raw approval value.</param>
    /// <param name="expected">Whether the value marks the entry as disabled.</param>
    [Theory]
    [InlineData(new byte[0], false)]
    [InlineData(new byte[] { 0x02, 0x00, 0x00 }, false)]
    [InlineData(new byte[] { 0x03, 0x00, 0x00 }, true)]
    [InlineData(new byte[] { 0x06, 0x00 }, false)]
    [InlineData(new byte[] { 0x07, 0x00 }, true)]
    public void IsDisabled_UsesOddFirstByte(byte[] value, bool expected)
        => StartupApprovalPolicy.IsDisabled(value).Should().Be(expected);
}
