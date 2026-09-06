using System;
using System.Windows;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Claude;
using TokenHound.Infrastructure.Providers.Mock;

namespace TokenHound.App;

/// <summary>
/// Main application entry point orchestrating usage store, provider adapters, and HUD window.
/// </summary>
public partial class App : Application
{
    private UsageStore? _usageStore;
    private NotchViewModel? _notchViewModel;
    private NotchWindow? _notchWindow;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _usageStore = new UsageStore(autoStart: true);

        var claudeProvider = new ClaudeOAuthProvider();
        _usageStore.RegisterProvider(claudeProvider);

        var claudeMonitor = new ClaudeSessionMonitor();
        _usageStore.RegisterActivityMonitor(claudeMonitor);

        var mockProvider = new MockUsageProvider();

        _notchViewModel = new NotchViewModel(
            _usageStore,
            action =>
            {
                if (Dispatcher.CheckAccess())
                    action();
                else
                    Dispatcher.Invoke(action);
            },
            mockProvider
        );

        _notchWindow = new NotchWindow
        {
            DataContext = _notchViewModel
        };

        _notchWindow.Show();

        _ = _usageStore.RefreshNowAsync();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {

        _notchViewModel?.Dispose();
        _usageStore?.Dispose();
        base.OnExit(e);
    }
}
