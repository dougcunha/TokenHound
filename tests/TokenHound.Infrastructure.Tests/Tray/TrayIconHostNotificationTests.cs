using AwesomeAssertions;
using System;
using System.Threading.Tasks;
using Serilog.Core;
using TokenHound.App.UI.Tray;
using TokenHound.App.ViewModels;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Tray;

/// <summary>
/// Verifies balloon notification forwarding and click dispatch in <see cref="TrayIconHost"/> (TC-21).
/// </summary>
public sealed class TrayIconHostNotificationTests
{
    /// <summary>
    /// Verifies that ShowNotification reaches the tray once initialized, is ignored before that and after a
    /// degraded start, and never throws when the tray fails (TC-21).
    /// </summary>
    [Fact]
    public void ShowNotification_ForwardsWhenInitialized_AndIsSafeOtherwise()
    {

        var fake = new FakeTrayIcon();
        var sut = new TrayIconHost(fake, CreateViewModel(), static a => a(), new Uri("pack://application:,,,/Assets/logo.ico"), Logger.None);

        sut.ShowNotification("early", "ignored");
        sut.Initialize();
        sut.ShowNotification("TokenHound update available", "1.3.0");
        fake.ThrowOnNotification = true;
        var act = () => sut.ShowNotification("boom", "boom");

        act.Should().NotThrow();
        fake.Notifications.Should().Equal(("TokenHound update available", "1.3.0"));
    }

    /// <summary>
    /// Verifies that a balloon click is dispatched to the UI thread and raised once, and stops after dispose (TC-21).
    /// </summary>
    [Fact]
    public void NotificationClicked_IsDispatchedAndRaised_UntilDisposed()
    {

        var fake = new FakeTrayIcon();
        var dispatched = 0;
        var sut = new TrayIconHost(
            fake,
            CreateViewModel(),
            a =>
            {
                dispatched++;
                a();
            },
            new Uri("pack://application:,,,/Assets/logo.ico"),
            Logger.None
        );
        var clicks = 0;
        sut.NotificationClicked += (_, _) => clicks++;
        sut.Initialize();

        fake.RaiseNotificationClicked();
        sut.Dispose();
        fake.RaiseNotificationClicked();

        clicks.Should().Be(1);
        dispatched.Should().Be(1);
        fake.HasEventSubscriptions.Should().BeFalse();
    }

    private static TrayIconViewModel CreateViewModel()
        => new(
            new HudActionsViewModel(
                static _ => Task.CompletedTask,
                static () => Task.CompletedTask,
                static () => { },
                static () => { }
            ),
            new NotchVisibilityController(static () => { }, static () => { }),
            new TrayMenuModel()
        );
}
