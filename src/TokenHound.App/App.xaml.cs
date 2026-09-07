using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Antigravity;
using TokenHound.Infrastructure.Providers.Claude;
using TokenHound.Infrastructure.Providers.Codex;
using TokenHound.Infrastructure.Providers.Copilot;
using TokenHound.Infrastructure.Providers.Cursor;

namespace TokenHound.App;

/// <summary>
/// Main application entry point orchestrating usage store, provider adapters, and HUD window.
/// </summary>
public partial class App : Application
{
    private UsageStore? _usageStore;
    private DialogService? _dialogService;
    private NotchViewModel? _notchViewModel;
    private HudActionsViewModel? _actionsViewModel;
    private ApplicationLifetime? _lifetime;
    private NotchWindow? _notchWindow;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _usageStore = new UsageStore(archive: new UsageArchive());

        var disposableResources = new List<IDisposable>();

        var claudeProvider = new ClaudeOAuthProvider();
        _usageStore.RegisterProvider(claudeProvider);

        var claudeMonitor = new ClaudeSessionMonitor();
        _usageStore.RegisterActivityMonitor(claudeMonitor);

        var antigravityProvider = new AntigravityUsageProvider();
        _usageStore.RegisterProvider(antigravityProvider);
        disposableResources.Add(antigravityProvider);

        var antigravityMonitor = new AntigravityActivityMonitor();
        _usageStore.RegisterActivityMonitor(antigravityMonitor);

        var codexProvider = new CodexUsageProvider();
        _usageStore.RegisterProvider(codexProvider);

        var codexMonitor = new CodexActivityMonitor();
        _usageStore.RegisterActivityMonitor(codexMonitor);

        var cursorProvider = new CursorUsageProvider();
        _usageStore.RegisterProvider(cursorProvider);
        disposableResources.Add(cursorProvider);

        var cursorMonitor = new CursorActivityMonitor();
        _usageStore.RegisterActivityMonitor(cursorMonitor);

        var copilotProvider = new CopilotUsageProvider();
        _usageStore.RegisterProvider(copilotProvider);
        disposableResources.Add(copilotProvider);

        var copilotMonitor = new CopilotActivityMonitor();
        _usageStore.RegisterActivityMonitor(copilotMonitor);
        disposableResources.Add(copilotMonitor);

        _dialogService = new DialogService(() => _notchWindow);

        _notchViewModel = new NotchViewModel(
            _usageStore,
            action =>
            {
                if (Dispatcher.CheckAccess())
                    action();
                else
                    Dispatcher.Invoke(action);
            }
        );

        _lifetime = new ApplicationLifetime(
            _usageStore,
            _dialogService,
            _notchViewModel,
            disposableResources
        );

        _actionsViewModel = new HudActionsViewModel(
            _usageStore.RefreshNowAsync,
            _lifetime.ShutdownAsync,
            () => _dialogService.ShowSettings(_notchWindow),
            () => _dialogService.ShowAbout(_notchWindow),
            () => _usageStore.CurrentSnapshots.Values
        );

        _notchWindow = new NotchWindow
        {
            DataContext = _notchViewModel,
            ActionsViewModel = _actionsViewModel
        };

        MainWindow = _notchWindow;
        _notchWindow.Show();

        _usageStore.Start();
        var startupTask = _usageStore.RefreshNowAsync(_lifetime.LifetimeToken);
        _lifetime.TrackStartupTask(startupTask);
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {

        _lifetime?.Dispose();
        base.OnExit(e);
    }
}
