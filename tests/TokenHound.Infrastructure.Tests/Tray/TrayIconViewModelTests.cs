using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TokenHound.App.UI.Tray;
using TokenHound.App.ViewModels;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Tray;

/// <summary>
/// Verifies header resolution, visibility change notifications, action delegation, refresh guards,
/// exit idempotency, and structured logging in <see cref="TrayIconViewModel"/> (TC-04, TC-06, TC-07, TC-08, TC-14).
/// </summary>
public sealed class TrayIconViewModelTests
{
    /// <summary>
    /// Verifies that constructor throws <see cref="ArgumentNullException"/> when required arguments are null.
    /// </summary>
    [Fact]
    public void Constructor_WhenRequiredArgumentsNull_ThrowsArgumentNullException()
    {

        var hudActions = CreateHudActions();
        var visibility = new NotchVisibilityController(() => { }, () => { });
        var menu = new TrayMenuModel();

        var act1 = () => new TrayIconViewModel(null!, visibility, menu);
        var act2 = () => new TrayIconViewModel(hudActions, null!, menu);
        var act3 = () => new TrayIconViewModel(hudActions, visibility, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that ToggleHeader reflects Notch visibility transitions and raises PropertyChanged (TC-04).
    /// </summary>
    [Fact]
    public void ToggleHeader_TracksVisibilityTransitions_AndRaisesPropertyChanged()
    {

        var visibility = new NotchVisibilityController(() => { }, () => { }, initiallyVisible: true);
        var sut = new TrayIconViewModel(CreateHudActions(), visibility, new TrayMenuModel());

        sut.ToggleHeader.Should().Be("Hide Notch");
        sut.BuildMenu().Entries[0].Header.Should().Be("Hide Notch");

        var propertyChanges = new List<string?>();
        sut.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);

        sut.ToggleNotch();

        sut.ToggleHeader.Should().Be("Show Notch");
        sut.BuildMenu().Entries[0].Header.Should().Be("Show Notch");
        propertyChanges.Should().ContainSingle().Which.Should().Be(nameof(TrayIconViewModel.ToggleHeader));

        sut.ToggleNotch();

        sut.ToggleHeader.Should().Be("Hide Notch");
        sut.BuildMenu().Entries[0].Header.Should().Be("Hide Notch");
        propertyChanges.Count.Should().Be(2);
        propertyChanges[1].Should().Be(nameof(TrayIconViewModel.ToggleHeader));
    }

    /// <summary>
    /// Verifies that concurrent RefreshNow calls reuse the underlying refresh guard and dispatch only once (TC-06).
    /// </summary>
    [Fact]
    public async Task RefreshNow_WhenAlreadyRefreshing_DispatchesUnderlyingRefreshOnlyOnce()
    {

        var tcs = new TaskCompletionSource();
        var refreshCount = 0;

        var hudActions = new HudActionsViewModel(
            _ =>
            {
                refreshCount++;
                return tcs.Task;
            },
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

        var visibility = new NotchVisibilityController(() => { }, () => { });
        var sut = new TrayIconViewModel(hudActions, visibility, new TrayMenuModel());

        var firstRefresh = sut.RefreshNow();
        var secondRefresh = sut.RefreshNow();

        refreshCount.Should().Be(1);

        tcs.SetResult();
        await firstRefresh;
        await secondRefresh;
    }

    /// <summary>
    /// Verifies that ShowSettings and ShowAbout execute before Exit and become no-ops after Exit (TC-07).
    /// </summary>
    [Fact]
    public async Task ShowSettingsAndShowAbout_CallDelegatesBeforeExit_AndAreNoOpsAfterExit()
    {

        var settingsCount = 0;
        var aboutCount = 0;
        var shutdownCount = 0;

        var hudActions = new HudActionsViewModel(
            _ => Task.CompletedTask,
            () =>
            {
                shutdownCount++;
                return Task.CompletedTask;
            },
            () => settingsCount++,
            () => aboutCount++
        );

        var visibility = new NotchVisibilityController(() => { }, () => { });
        var sut = new TrayIconViewModel(hudActions, visibility, new TrayMenuModel());

        sut.ShowSettings();
        sut.ShowAbout();

        settingsCount.Should().Be(1);
        aboutCount.Should().Be(1);

        await sut.Exit();
        shutdownCount.Should().Be(1);

        sut.ShowSettings();
        sut.ShowAbout();

        settingsCount.Should().Be(1);
        aboutCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that Invoke(Exit) calls the shutdown delegate once, and no other key reaches shutdown (TC-08).
    /// </summary>
    [Fact]
    public void Invoke_ExitTwice_CallsShutdownDelegateOnce_AndOtherKeysNeverCallShutdown()
    {

        var shutdownCount = 0;

        var hudActions = new HudActionsViewModel(
            _ => Task.CompletedTask,
            () =>
            {
                shutdownCount++;
                return Task.CompletedTask;
            },
            () => { },
            () => { }
        );

        var visibility = new NotchVisibilityController(() => { }, () => { });
        var sut = new TrayIconViewModel(hudActions, visibility, new TrayMenuModel());

        sut.Invoke(TrayMenuItemKey.ToggleNotch);
        sut.Invoke(TrayMenuItemKey.RefreshNow);
        sut.Invoke(TrayMenuItemKey.Settings);
        sut.Invoke(TrayMenuItemKey.About);

        shutdownCount.Should().Be(0);

        sut.Invoke(TrayMenuItemKey.Exit);
        shutdownCount.Should().Be(1);

        sut.Invoke(TrayMenuItemKey.Exit);
        shutdownCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that invoking each menu item emits a structured Serilog entry with its nameof action token (TC-14).
    /// </summary>
    [Fact]
    public void Invoke_EachAction_EmitsStructuredSerilogEntryWithStateToken()
    {

        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var hudActions = CreateHudActions();
        var visibility = new NotchVisibilityController(() => { }, () => { });
        var sut = new TrayIconViewModel(hudActions, visibility, new TrayMenuModel(), logger);

        sut.Invoke(TrayMenuItemKey.ToggleNotch);
        sut.Invoke(TrayMenuItemKey.RefreshNow);
        sut.Invoke(TrayMenuItemKey.Settings);
        sut.Invoke(TrayMenuItemKey.About);
        sut.Invoke(TrayMenuItemKey.Exit);

        sink.Events.Count.Should().Be(5);

        var expectedActions = new[]
        {
            nameof(TrayIconViewModel.ToggleNotch),
            nameof(TrayIconViewModel.RefreshNow),
            nameof(TrayIconViewModel.ShowSettings),
            nameof(TrayIconViewModel.ShowAbout),
            nameof(TrayIconViewModel.Exit)
        };

        for (var i = 0; i < expectedActions.Length; i++)
        {

            sink.Events[i].Properties.Should().ContainKey("Action");
            sink.Events[i].Properties["Action"].ToString().Trim('"').Should().Be(expectedActions[i]);
        }
    }

    private static HudActionsViewModel CreateHudActions()
        => new(
            _ => Task.CompletedTask,
            () => Task.CompletedTask,
            () => { },
            () => { }
        );

    private sealed class TestLogSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {

            Events.Add(logEvent);
        }
    }
}
