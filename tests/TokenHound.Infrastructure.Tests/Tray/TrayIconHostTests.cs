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
/// Verifies initialization, idempotent disposal, graceful degradation, dispatcher marshalling,
/// and structured logging in <see cref="TrayIconHost"/> (TC-01, TC-12, TC-13, TC-14).
/// </summary>
public sealed class TrayIconHostTests
{
    /// <summary>
    /// Verifies that constructor throws <see cref="ArgumentNullException"/> when any required argument is null.
    /// </summary>
    [Fact]
    public void Constructor_WhenRequiredArgumentsNull_ThrowsArgumentNullException()
    {

        var fake = new FakeTrayIcon();
        var vm = CreateViewModel();
        var uri = new Uri("pack://application:,,,/Assets/logo.ico");
        var logger = new LoggerConfiguration().CreateLogger();
        Action<Action> dispatch = static a => a();

        var act1 = () => new TrayIconHost(null!, vm, dispatch, uri, logger);
        var act2 = () => new TrayIconHost(fake, null!, dispatch, uri, logger);
        var act3 = () => new TrayIconHost(fake, vm, null!, uri, logger);
        var act4 = () => new TrayIconHost(fake, vm, dispatch, null!, logger);
        var act5 = () => new TrayIconHost(fake, vm, dispatch, uri, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
        act5.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Initialize displays the icon once, second Initialize is a no-op, and Dispose is idempotent (TC-01).
    /// </summary>
    [Fact]
    public void Initialize_AndDispose_AreIdempotent_AndManageIconLifetime()
    {

        var fake = new FakeTrayIcon();
        var vm = CreateViewModel();
        var uri = new Uri("pack://application:,,,/Assets/logo.ico");
        var logger = new LoggerConfiguration().CreateLogger();
        var sut = new TrayIconHost(fake, vm, static a => a(), uri, logger);

        sut.Initialize();

        fake.ShowCallCount.Should().Be(1);
        fake.LastTooltip.Should().Be("TokenHound");
        fake.LastIconSource.Should().Be(uri);
        fake.LastMenu.Should().NotBeNull();
        fake.HasEventSubscriptions.Should().BeTrue();

        sut.Initialize();

        fake.ShowCallCount.Should().Be(1);

        sut.Dispose();

        fake.DisposeCallCount.Should().Be(1);
        fake.HasEventSubscriptions.Should().BeFalse();

        sut.Dispose();

        fake.DisposeCallCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that a throwing Show catches the exception, logs a warning, sets IsDegraded, and skips events (TC-12).
    /// </summary>
    [Fact]
    public void Initialize_WhenShowThrows_CatchesException_LogsWarning_AndDegradesGracefully()
    {

        var fake = new FakeTrayIcon { ThrowOnShow = true };
        var vm = CreateViewModel();
        var uri = new Uri("pack://application:,,,/Assets/logo.ico");
        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var sut = new TrayIconHost(fake, vm, static a => a(), uri, logger);

        var act = () => sut.Initialize();

        act.Should().NotThrow();
        sut.IsDegraded.Should().BeTrue();
        fake.HasEventSubscriptions.Should().BeFalse();

        var warnings = sink.Events.FindAll(static e => e.Level == LogEventLevel.Warning);
        warnings.Count.Should().Be(1);
        warnings[0].Exception.Should().NotBeNull();
        warnings[0].Exception!.Message.Should().Be("Failed to register notification icon.");
    }

    /// <summary>
    /// Verifies that every tray click and menu callback runs through the injected dispatcher (TC-13).
    /// </summary>
    [Fact]
    public void TrayCallbacks_AreMarshalledThroughDispatcherSpy()
    {

        var fake = new FakeTrayIcon();
        var visibility = new NotchVisibilityController(static () => { }, static () => { });
        var vm = new TrayIconViewModel(CreateHudActions(), visibility, new TrayMenuModel());
        var uri = new Uri("pack://application:,,,/Assets/logo.ico");
        var logger = new LoggerConfiguration().CreateLogger();

        var dispatchedCount = 0;
        Action<Action> dispatch = a =>
        {
            dispatchedCount++;
            a();
        };

        var sut = new TrayIconHost(fake, vm, dispatch, uri, logger);
        sut.Initialize();

        fake.RaiseLeftClicked();
        dispatchedCount.Should().Be(1);

        fake.RaiseDoubleClicked();
        dispatchedCount.Should().Be(2);

        fake.RaiseMenuItemInvoked(TrayMenuItemKey.RefreshNow);
        dispatchedCount.Should().Be(3);

        fake.RaiseMenuOpening();
        dispatchedCount.Should().Be(4);
        fake.LastToggleHeader.Should().Be("Hide Notch");
    }

    /// <summary>
    /// Verifies that structured Serilog entries are emitted for created, removed, failed, and action events (TC-14).
    /// </summary>
    [Fact]
    public void Logging_EmitsStructuredEntries_ForCreatedRemovedFailedAndActions()
    {

        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var fake = new FakeTrayIcon();
        var vm = CreateViewModel();
        var uri = new Uri("pack://application:,,,/Assets/logo.ico");
        var sut = new TrayIconHost(fake, vm, static a => a(), uri, logger);

        sut.Initialize();

        var createdLog = sink.Events.Find(static e => e.Level == LogEventLevel.Information && e.Properties.ContainsKey("State") && e.Properties["State"].ToString().Trim('"') == nameof(TrayIconHost.Initialize));
        createdLog.Should().NotBeNull();
        createdLog!.Properties.Should().ContainKey("Tooltip");
        createdLog.Properties["Tooltip"].ToString().Trim('"').Should().Be("TokenHound");

        fake.RaiseLeftClicked();
        fake.RaiseDoubleClicked();
        fake.RaiseMenuItemInvoked(TrayMenuItemKey.ToggleNotch);
        fake.RaiseMenuItemInvoked(TrayMenuItemKey.RefreshNow);
        fake.RaiseMenuItemInvoked(TrayMenuItemKey.Settings);
        fake.RaiseMenuItemInvoked(TrayMenuItemKey.About);
        fake.RaiseMenuItemInvoked(TrayMenuItemKey.Exit);

        sut.Dispose();

        var removedLog = sink.Events.Find(static e => e.Level == LogEventLevel.Information && e.Properties.ContainsKey("State") && e.Properties["State"].ToString().Trim('"') == nameof(TrayIconHost.Dispose));
        removedLog.Should().NotBeNull();

        var failedSink = new TestLogSink();
        var failedLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(failedSink)
            .CreateLogger();

        var failingFake = new FakeTrayIcon { ThrowOnShow = true };
        var degradedSut = new TrayIconHost(failingFake, vm, static a => a(), uri, failedLogger);
        degradedSut.Initialize();

        var warningLog = failedSink.Events.Find(static e => e.Level == LogEventLevel.Warning);
        warningLog.Should().NotBeNull();
        warningLog!.Exception.Should().NotBeNull();
    }

    private static TrayIconViewModel CreateViewModel()
        => new(
            CreateHudActions(),
            new NotchVisibilityController(static () => { }, static () => { }),
            new TrayMenuModel()
        );

    private static HudActionsViewModel CreateHudActions()
        => new(
            static _ => Task.CompletedTask,
            static () => Task.CompletedTask,
            static () => { },
            static () => { }
        );

    private sealed class FakeTrayIcon : ITrayIcon
    {
        public int ShowCallCount { get; private set; }

        public int DisposeCallCount { get; private set; }

        public Uri? LastIconSource { get; private set; }

        public string? LastTooltip { get; private set; }

        public TrayMenuDescriptor? LastMenu { get; private set; }

        public string? LastToggleHeader { get; private set; }

        public bool ThrowOnShow { get; set; }

        public event EventHandler? LeftClicked;

        public event EventHandler? DoubleClicked;

        public event EventHandler? MenuOpening;

        public event EventHandler<TrayMenuItemKey>? MenuItemInvoked;

        public bool HasEventSubscriptions
            => LeftClicked is not null || DoubleClicked is not null || MenuOpening is not null || MenuItemInvoked is not null;

        public void Show(Uri iconSource, string tooltip, TrayMenuDescriptor menu)
        {

            ShowCallCount++;
            LastIconSource = iconSource;
            LastTooltip = tooltip;
            LastMenu = menu;

            if (ThrowOnShow)
                throw new InvalidOperationException("Failed to register notification icon.");
        }

        public void UpdateToggleHeader(string header)
        {

            LastToggleHeader = header;
        }

        public void RaiseLeftClicked()
            => LeftClicked?.Invoke(this, EventArgs.Empty);

        public void RaiseDoubleClicked()
            => DoubleClicked?.Invoke(this, EventArgs.Empty);

        public void RaiseMenuOpening()
            => MenuOpening?.Invoke(this, EventArgs.Empty);

        public void RaiseMenuItemInvoked(TrayMenuItemKey key)
            => MenuItemInvoked?.Invoke(this, key);

        public void Dispose()
        {

            DisposeCallCount++;
        }
    }

    private sealed class TestLogSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {

            Events.Add(logEvent);
        }
    }
}
