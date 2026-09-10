using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Logging;
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
    private readonly ProviderSettingsStore _providerSettingsStore = new();

    private RateLimitPolicy? _rateLimitPolicy;
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

        InitializeLogging();
        ConfigureExceptionHandling();

        var disposableResources = new List<IDisposable>();
        var archive = new UsageArchive();
        disposableResources.Add(archive);

        _rateLimitPolicy = InitializeRateLimits();

        _usageStore = CreateUsageStore(archive);

        RegisterProviders(
            _usageStore,
            archive,
            _rateLimitPolicy,
            disposableResources
        );
        ApplyProviderEnablement(_usageStore);
        InitializeUi(_usageStore, disposableResources);
        ScheduleInitialRefresh();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {

        Log.Information("TokenHound exiting with code {ExitCode}", e.ApplicationExitCode);

        _lifetime?.Dispose();
        Log.CloseAndFlush();

        base.OnExit(e);
    }

    private static void InitializeLogging()
    {

        var settings = LogConfigurationLoader.Load();
        var appInfo = ApplicationInfo.Current;

        LoggingBootstrapper.Initialize(settings, appInfo.Name);
        LoggingBootstrapper.LogHeader(Log.Logger, appInfo.Name, appInfo.DisplayVersion);

        Log.Information("Starting TokenHound application initialization...");
    }

    private void ConfigureExceptionHandling()
    {

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {

        Log.Fatal(e.Exception, "Unhandled dispatcher exception encountered");
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {

        if (e.ExceptionObject is Exception ex)
            Log.Fatal(ex, "Unhandled AppDomain exception encountered");
        else
            Log.Fatal("Unhandled AppDomain error: {ExceptionObject}", e.ExceptionObject);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {

        Log.Error(e.Exception, "Unobserved task exception encountered");
        e.SetObserved();
    }

    private static RateLimitPolicy InitializeRateLimits()
    {

        var rateLimitSettings = new RateLimitSettingsStore().Load();
        var rateLimitPolicy = new RateLimitPolicy(rateLimitSettings.MinimumRetryFloor);

        Log.Information(
            "Rate limit retry floor resolved: {MinimumRetryFloorSeconds}s",
            rateLimitSettings.MinimumRetryFloor.TotalSeconds
        );

        return rateLimitPolicy;
    }

    private static UsageStore CreateUsageStore(UsageArchive archive)
    {

        var settings = new RefreshSettingsStore().Load();

        Log.Information(
            "Polling cadence resolved: active {ActiveInterval}, idle {IdleInterval}",
            settings.ActiveInterval,
            settings.IdleInterval
        );

        return new UsageStore(
            autoStart: true,
            idleInterval: settings.IdleInterval,
            pollInterval: settings.ActiveInterval,
            archive: archive
        );
    }

    private static void RegisterProviders(
        UsageStore usageStore,
        UsageArchive archive,
        RateLimitPolicy rateLimitPolicy,
        List<IDisposable> disposableResources)
    {

        Log.Information("Registering provider adapters and activity monitors...");

        RegisterClaude(usageStore, rateLimitPolicy);
        RegisterAntigravity(usageStore, rateLimitPolicy, disposableResources);
        RegisterCodex(usageStore);
        RegisterCursor(usageStore, rateLimitPolicy, disposableResources);
        RegisterCopilot(
            usageStore,
            archive,
            rateLimitPolicy,
            disposableResources
        );
    }

    private static void RegisterClaude(UsageStore usageStore, RateLimitPolicy rateLimitPolicy)
    {

        var provider = new ClaudeOAuthProvider(rateLimitPolicy: rateLimitPolicy);
        usageStore.RegisterProvider(provider);
        usageStore.RegisterActivityMonitor(new ClaudeSessionMonitor());
    }

    private static void RegisterAntigravity(
        UsageStore usageStore,
        RateLimitPolicy rateLimitPolicy,
        List<IDisposable> disposableResources)
    {

        var provider = new AntigravityUsageProvider(rateLimitPolicy: rateLimitPolicy);
        usageStore.RegisterProvider(provider);
        usageStore.RegisterActivityMonitor(new AntigravityActivityMonitor());
        disposableResources.Add(provider);
    }

    private static void RegisterCodex(UsageStore usageStore)
    {

        var provider = new CodexUsageProvider();
        usageStore.RegisterProvider(provider);
        usageStore.RegisterActivityMonitor(new CodexActivityMonitor());
    }

    private static void RegisterCursor(
        UsageStore usageStore,
        RateLimitPolicy rateLimitPolicy,
        List<IDisposable> disposableResources)
    {

        var provider = new CursorUsageProvider(rateLimitPolicy: rateLimitPolicy);
        usageStore.RegisterProvider(provider);
        usageStore.RegisterActivityMonitor(new CursorActivityMonitor());
        disposableResources.Add(provider);
    }

    private static void RegisterCopilot(
        UsageStore usageStore,
        UsageArchive archive,
        RateLimitPolicy rateLimitPolicy,
        List<IDisposable> disposableResources)
    {

        var gate = new CopilotRequestGate(archive, rateLimitPolicy: rateLimitPolicy);
        var billingClient = new CopilotBillingClient(gate: gate);
        var billingService = new CopilotBillingService(
            billingClient,
            new CopilotBillingContextResolver(billingClient),
            archive,
            gate
        );
        var provider = CreateCopilotProvider(gate, billingService);
        usageStore.RegisterProvider(provider);
        var monitor = new CopilotActivityMonitor();
        usageStore.RegisterActivityMonitor(monitor);
        disposableResources.Add(provider);
        disposableResources.Add(billingService);
        disposableResources.Add(billingClient);
        disposableResources.Add(gate);
        disposableResources.Add(monitor);
    }

    private static CopilotUsageProvider CreateCopilotProvider(
        CopilotRequestGate gate,
        CopilotBillingService billingService)
        => new(
            null,
            null,
            gate,
            billingService
        );

    /// <summary>
    /// Applies the persisted monitoring preferences to every registered provider. Runs after registration so the
    /// identifiers are known, and before the HUD view model is built so a disabled provider never gets a ring.
    /// </summary>
    /// <param name="usageStore">The usage store owning the provider registry and the enablement gate.</param>
    private void ApplyProviderEnablement(UsageStore usageStore)
    {

        var settings = _providerSettingsStore.Load();

        Log.Information(
            "Resolving provider enablement from {SettingsFilePath}",
            _providerSettingsStore.FilePath
        );

        foreach (var providerId in usageStore.RegisteredProviderIds)
        {

            var isEnabled = settings.IsEnabled(providerId);

            usageStore.SetProviderEnabled(providerId, isEnabled);

            Log.Information(
                "Provider enablement resolved: {ProviderId} monitored {IsEnabled}",
                providerId,
                isEnabled
            );
        }
    }

    private SettingsViewModel CreateSettingsViewModel(UsageStore usageStore)
        => new(
            usageStore,
            _providerSettingsStore.SaveAsync,
            DispatchUiAction,
            new CadenceSettingsViewModel(
                usageStore,
                new RefreshSettingsStore(),
                new RateLimitSettingsStore(),
                _rateLimitPolicy ?? throw new InvalidOperationException("Rate-limit policy is not initialized.")
            )
        );

    private void InitializeUi(UsageStore usageStore, List<IDisposable> disposableResources)
    {

        Log.Information("Initializing HUD window and presentation models...");

        _dialogService = new DialogService(() => _notchWindow);
        _notchViewModel = new NotchViewModel(usageStore, DispatchUiAction);
        _lifetime = new ApplicationLifetime(usageStore, _dialogService, _notchViewModel, disposableResources);

        _actionsViewModel = new HudActionsViewModel(
            usageStore.RefreshNowAsync,
            _lifetime.ShutdownAsync,
            () => _dialogService.ShowSettings(_notchWindow, () => CreateSettingsViewModel(usageStore)),
            () => _dialogService.ShowAbout(_notchWindow),
            () => usageStore.CurrentSnapshots.Values
        );

        _notchWindow = new NotchWindow
        {
            DataContext = _notchViewModel,
            ActionsViewModel = _actionsViewModel
        };

        MainWindow = _notchWindow;
        _notchWindow.Show();

        Log.Information("HUD window displayed successfully.");
    }

    private void DispatchUiAction(Action action)
    {

        if (Dispatcher.CheckAccess())
            action();
        else
            Dispatcher.Invoke(action);
    }

    private void ScheduleInitialRefresh()
    {

        if (_usageStore is null || _lifetime is null)
            return;

        Log.Information("Dispatching initial usage refresh in background...");

        var startupTask = _usageStore.RefreshNowAsync(_lifetime.LifetimeToken);
        _lifetime.TrackStartupTask(startupTask);
    }
}
