using AwesomeAssertions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Startup;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the Apply pattern and failure handling of <see cref="StartupSettingsViewModel"/> over a fake service (TC-07..TC-09).
/// </summary>
public sealed class StartupSettingsViewModelTests
{
    private const string EXECUTABLE_PATH = @"C:\Apps\TokenHound\TokenHound.App.exe";

    /// <summary>
    /// Verifies that Apply is offered only while the checkbox differs from the operating system state (TC-07).
    /// </summary>
    [Fact]
    public void CanApply_TracksDifferenceFromOperatingSystemState()
    {

        var sut = new StartupSettingsViewModel(new FakeStartupService(), EXECUTABLE_PATH);

        sut.IsEnabled.Should().BeFalse();
        sut.CanApply.Should().BeFalse();
        sut.ApplyCommand.CanExecute(null).Should().BeFalse();

        sut.IsEnabled = true;

        sut.CanApply.Should().BeTrue();
        sut.ApplyCommand.CanExecute(null).Should().BeTrue();

        sut.IsEnabled = false;

        sut.CanApply.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that applying on enables the shortcut for the executable and reads the state back (TC-08).
    /// </summary>
    [Fact]
    public void Apply_WhenTurnedOn_EnablesAndReadsStateBack()
    {

        var service = new FakeStartupService();
        var sut = new StartupSettingsViewModel(service, EXECUTABLE_PATH) { IsEnabled = true };

        sut.ApplyCommand.Execute(null);

        service.EnabledPath.Should().Be(EXECUTABLE_PATH);
        service.IsEnabledCalls.Should().Be(2);
        sut.IsEnabled.Should().BeTrue();
        sut.IsApplied.Should().BeTrue();
        sut.CanApply.Should().BeFalse();
        sut.ApplyError.Should().BeNull();
    }

    /// <summary>
    /// Verifies that applying off disables the shortcut and reads the state back (TC-08).
    /// </summary>
    [Fact]
    public void Apply_WhenTurnedOff_DisablesAndReadsStateBack()
    {

        var service = new FakeStartupService { State = true };
        var sut = new StartupSettingsViewModel(service, EXECUTABLE_PATH) { IsEnabled = false };

        sut.Apply();

        service.DisableCalls.Should().Be(1);
        sut.IsEnabled.Should().BeFalse();
        sut.IsApplied.Should().BeTrue();
        sut.CanApply.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a failing service shows the error and keeps the checkbox on the state read back (TC-09).
    /// </summary>
    /// <param name="failure">The kind of exception the service throws.</param>
    [Theory]
    [InlineData(nameof(UnauthorizedAccessException))]
    [InlineData(nameof(IOException))]
    [InlineData(nameof(COMException))]
    public void Apply_WhenServiceFails_ShowsErrorAndKeepsRealState(string failure)
    {

        var service = new FakeStartupService { Failure = CreateFailure(failure) };
        var sut = new StartupSettingsViewModel(service, EXECUTABLE_PATH) { IsEnabled = true };

        var apply = () => sut.Apply();

        apply.Should().NotThrow();
        sut.ApplyError.Should().Be(StartupSettingsViewModel.APPLY_ERROR);
        sut.IsEnabled.Should().BeFalse();
        sut.IsApplied.Should().BeFalse();
        sut.CanApply.Should().BeFalse();
    }

    private static Exception CreateFailure(string failure)
        => failure switch
        {
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException("denied"),
            nameof(IOException) => new IOException("locked"),
            _ => new COMException("shell failure")
        };

    private sealed class FakeStartupService : IStartupLaunchService
    {
        public bool State { get; set; }

        public Exception? Failure { get; init; }

        public string? EnabledPath { get; private set; }

        public int IsEnabledCalls { get; private set; }

        public int DisableCalls { get; private set; }

        public bool IsEnabled()
        {

            IsEnabledCalls++;

            return State;
        }

        public void Enable(string executablePath)
        {

            if (Failure is not null)
                throw Failure;

            EnabledPath = executablePath;
            State = true;
        }

        public void Disable()
        {

            if (Failure is not null)
                throw Failure;

            DisableCalls++;
            State = false;
        }
    }
}
