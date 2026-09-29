using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using TokenHound.Infrastructure.Startup;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Startup;

/// <summary>
/// Verifies the startup shortcut service against a temporary Startup folder and a fake approval store (TC-02..TC-06).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class StartupLaunchServiceTests : IDisposable
{
    // Coarse stall guard only: it includes the first COM activation, and CI runners can be loaded.
    // The 200 ms budget of NFR-03 is checked manually (MA-1).
    private const long STALL_GUARD_MS = 2000;

    private static readonly byte[] DISABLED_VALUE = [0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"startup_test_{Guid.NewGuid():N}");
    private readonly FakeApprovalStore _approvalStore = new();
    private readonly StartupLaunchService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLaunchServiceTests"/> class.
    /// </summary>
    public StartupLaunchServiceTests()
        => _service = new StartupLaunchService(Path.Combine(_rootPath, "Startup"), _approvalStore);

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    /// <summary>
    /// Verifies that the default service resolves the Startup folder without requiring it to exist (codereview_1/CR-01).
    /// </summary>
    [Fact]
    public void CreateDefault_ResolvesStartupFolderWithoutVerification()
    {

        var expectedFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup, Environment.SpecialFolderOption.DoNotVerify);

        StartupLaunchService.ResolveStartupFolder().Should().Be(expectedFolder);
        Path.IsPathRooted(expectedFolder).Should().BeTrue();
        StartupLaunchService.CreateDefault().ShortcutPath.Should().Be(Path.Combine(expectedFolder, StartupLaunchService.SHORTCUT_FILE_NAME));
    }

    /// <summary>
    /// Verifies that a Startup folder without the shortcut reads as disabled (TC-02).
    /// </summary>
    [Fact]
    public void IsEnabled_WhenShortcutMissing_ReturnsFalse()
        => _service.IsEnabled().Should().BeFalse();

    /// <summary>
    /// Verifies that enabling creates the folder and a shortcut targeting the executable and its folder, quickly (TC-03).
    /// </summary>
    [Fact]
    public void Enable_CreatesShortcutToExecutable()
    {

        var executablePath = CreateExecutable("a");
        var stopwatch = Stopwatch.StartNew();

        _service.Enable(executablePath);

        var enabled = _service.IsEnabled();

        stopwatch.Stop();

        enabled.Should().BeTrue();
        Path.GetFileName(_service.ShortcutPath).Should().Be(StartupLaunchService.SHORTCUT_FILE_NAME);
        ShellLink.ReadTarget(_service.ShortcutPath).Should().BeEquivalentTo(executablePath);
        ShellLink.ReadWorkingDirectory(_service.ShortcutPath).Should().BeEquivalentTo(Path.GetDirectoryName(executablePath));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(STALL_GUARD_MS);
    }

    /// <summary>
    /// Verifies that enabling over an existing shortcut rewrites its target (TC-04).
    /// </summary>
    [Fact]
    public void Enable_WhenShortcutTargetsAnotherExecutable_RewritesTarget()
    {

        _service.Enable(CreateExecutable("a"));

        var newTarget = CreateExecutable("b");

        _service.Enable(newTarget);

        ShellLink.ReadTarget(_service.ShortcutPath).Should().BeEquivalentTo(newTarget);
    }

    /// <summary>
    /// Verifies that a Windows "Startup apps" disabled flag reads as disabled and enabling clears it (TC-05).
    /// </summary>
    [Fact]
    public void Enable_WhenWindowsDisabledEntry_ClearsFlag()
    {

        var executablePath = CreateExecutable("a");

        _service.Enable(executablePath);
        _approvalStore.Values[StartupLaunchService.SHORTCUT_FILE_NAME] = DISABLED_VALUE;

        _service.IsEnabled().Should().BeFalse();

        _service.Enable(executablePath);

        _approvalStore.Values.Should().NotContainKey(StartupLaunchService.SHORTCUT_FILE_NAME);
        _service.IsEnabled().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that disabling deletes the shortcut and is harmless when it is already absent (TC-06).
    /// </summary>
    [Fact]
    public void Disable_DeletesShortcutAndToleratesMissingShortcut()
    {

        _service.Enable(CreateExecutable("a"));

        _service.Disable();

        File.Exists(_service.ShortcutPath).Should().BeFalse();
        _service.IsEnabled().Should().BeFalse();

        var repeat = () => _service.Disable();

        repeat.Should().NotThrow();
    }

    private string CreateExecutable(string folderName)
    {

        var folder = Path.Combine(_rootPath, folderName);

        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, "TokenHound.App.exe");

        File.WriteAllBytes(path, []);

        return path;
    }

    private sealed class FakeApprovalStore : IStartupApprovalStore
    {
        public Dictionary<string, byte[]> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public byte[]? ReadValue(string name)
            => Values.GetValueOrDefault(name);

        public void DeleteValue(string name)
            => Values.Remove(name);
    }
}
