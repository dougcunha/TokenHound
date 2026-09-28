using AwesomeAssertions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies the silent installer arguments and start failures of <see cref="InstallerUpdateLauncher"/> (TC-15).
/// </summary>
public sealed class InstallerUpdateLauncherTests : IDisposable
{
    private readonly string _setupPath = Path.Combine(Path.GetTempPath(), $"TokenHound-Setup-test-{Guid.NewGuid():N}.exe");

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallerUpdateLauncherTests"/> class.
    /// </summary>
    public InstallerUpdateLauncherTests()
    {

        File.WriteAllText(_setupPath, "setup");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        File.Delete(_setupPath);
    }

    /// <summary>
    /// Verifies the exact arguments and the absolute path.
    /// </summary>
    [Fact]
    public void Launch_StartsSetupSilentlyWithRelaunch()
    {

        ProcessStartInfo? captured = null;
        var launcher = new InstallerUpdateLauncher(info =>
        {
            captured = info;

            return true;
        });

        launcher.Launch(_setupPath);

        captured!.FileName.Should().Be(Path.GetFullPath(_setupPath));
        Path.IsPathFullyQualified(captured.FileName).Should().BeTrue();
        captured.UseShellExecute.Should().BeFalse();
        captured.ArgumentList.Should().Equal("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/RELAUNCH=1");
    }

    /// <summary>
    /// Verifies that a start failure or a missing file surfaces as an apply failure.
    /// </summary>
    [Fact]
    public void Launch_WhenStartFailsOrFileMissing_ThrowsApplyException()
    {

        var notStarted = new InstallerUpdateLauncher(static _ => false);
        var throwing = new InstallerUpdateLauncher(static _ => throw new Win32Exception(5));
        var missing = new InstallerUpdateLauncher(static _ => true);

        FluentActions.Invoking(() => notStarted.Launch(_setupPath)).Should().Throw<UpdateApplyException>();
        FluentActions.Invoking(() => throwing.Launch(_setupPath)).Should().Throw<UpdateApplyException>();
        FluentActions.Invoking(() => missing.Launch(_setupPath + ".missing")).Should().Throw<UpdateApplyException>();
    }
}
