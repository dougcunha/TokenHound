using System;
using System.Windows;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Antigravity;
using TokenHound.Infrastructure.Providers.Claude;
using TokenHound.Infrastructure.Providers.Codex;
using TokenHound.Infrastructure.Providers.Cursor;
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

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _usageStore = new UsageStore(autoStart: true);

        var claudeProvider = new ClaudeOAuthProvider();
        _usageStore.RegisterProvider(claudeProvider);

        var claudeMonitor = new ClaudeSessionMonitor();
        _usageStore.RegisterActivityMonitor(claudeMonitor);

        var antigravityProvider = new AntigravityUsageProvider();
        _usageStore.RegisterProvider(antigravityProvider);

        var antigravityMonitor = new AntigravityActivityMonitor();
        _usageStore.RegisterActivityMonitor(antigravityMonitor);

        var codexProvider = new CodexUsageProvider();
        _usageStore.RegisterProvider(codexProvider);

        var codexMonitor = new CodexActivityMonitor();
        _usageStore.RegisterActivityMonitor(codexMonitor);

        var cursorProvider = new CursorUsageProvider();
        _usageStore.RegisterProvider(cursorProvider);

        var cursorMonitor = new CursorActivityMonitor();
        _usageStore.RegisterActivityMonitor(cursorMonitor);

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

        MainWindow = _notchWindow;
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
