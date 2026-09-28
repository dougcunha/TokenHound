using System;
using System.Collections.Generic;
using TokenHound.App.UI.Tray;

namespace TokenHound.Infrastructure.Tests.Tray;

/// <summary>
/// Recording <see cref="ITrayIcon"/> that lets tests raise tray events and inspect what the host sent.
/// </summary>
internal sealed class FakeTrayIcon : ITrayIcon
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

    public event EventHandler? NotificationClicked;

    public List<(string Title, string Message)> Notifications { get; } = [];

    public bool ThrowOnNotification { get; set; }

    public bool HasEventSubscriptions
        => LeftClicked is not null
            || DoubleClicked is not null
            || MenuOpening is not null
            || MenuItemInvoked is not null
            || NotificationClicked is not null;

    public void ShowNotification(string title, string message)
    {

        if (ThrowOnNotification)
            throw new InvalidOperationException("Balloon failed.");

        Notifications.Add((title, message));
    }

    public void RaiseNotificationClicked()
        => NotificationClicked?.Invoke(this, EventArgs.Empty);

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
