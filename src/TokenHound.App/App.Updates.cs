using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Updates;

namespace TokenHound.App;

/// <summary>Composes the update services and opens the update dialog from the tray.</summary>
public partial class App
{
    private const string UPDATE_NOTIFICATION_TITLE = "TokenHound update available";

    private readonly UpdateDialog _updateDialog = new();

    private UpdateSettingsStore? _updateSettingsStore;
    private UpdateCheckService? _updateCheckService;
    private UpdateRequestGate? _updateGate;
    private UpdateCheckOutcome? _pendingUpdateOutcome;

    private void InitializeUpdates(List<IDisposable> disposableResources)
    {

        var currentVersion = RunningVersion.Resolve(typeof(App).Assembly);
        var settingsStore = new UpdateSettingsStore();
        var gate = new UpdateRequestGate(new UpdateStateStore());
        var client = new GitHubReleaseClient(currentVersion?.ToString() ?? UpdateMessages.UNKNOWN_VERSION);
        var service = new UpdateCheckService(
            client,
            gate,
            settingsStore,
            currentVersion
        );

        disposableResources.Add(service);
        disposableResources.Add(gate);
        disposableResources.Add(client);

        _updateSettingsStore = settingsStore;
        _updateCheckService = service;
        _updateGate = gate;
    }

    private UpdateSettingsViewModel? CreateUpdateSettingsViewModel()
        => _updateSettingsStore is null ? null : new UpdateSettingsViewModel(_updateSettingsStore);

    private void StartUpdateScheduler()
    {

        if (_updateCheckService is null || _updateSettingsStore is null || _updateGate is null || _lifetime is null)
            return;

        var service = _updateCheckService;
        var store = _updateSettingsStore;
        var gate = _updateGate;
        var scheduler = new UpdateCheckScheduler(
            store.Load,
            () => gate.LastCheckUtc,
            cancellationToken => service.CheckAsync(UpdateCheckTrigger.Scheduled, cancellationToken)
        );

        scheduler.UpdateAvailable += OnScheduledUpdateAvailable;
        _trayHost?.NotificationClicked += OnUpdateNotificationClicked;

        var lifetimeToken = _lifetime.LifetimeToken;
        _lifetime.TrackStartupTask(Task.Run(() => scheduler.RunAsync(lifetimeToken), lifetimeToken));
    }

    private void OnScheduledUpdateAvailable(object? sender, UpdateCheckOutcome outcome)
    {

        _pendingUpdateOutcome = outcome;

        var version = outcome.LatestVersion?.ToString() ?? outcome.Release?.TagName ?? string.Empty;

        DispatchUiAction(() => _trayHost?.ShowNotification(UPDATE_NOTIFICATION_TITLE, $"TokenHound {version} is available. Click to review."));
    }

    private void OnUpdateNotificationClicked(object? sender, EventArgs e)
    {

        var outcome = _pendingUpdateOutcome;

        if (outcome is null)
        {
            ShowUpdateDialog();

            return;
        }

        _updateDialog.Show(_notchWindow, CreateUpdateViewModel).ShowOutcome(outcome);
    }

    private void ShowUpdateDialog()
    {

        if (_updateCheckService is null)
        {
            Log.Warning("Update dialog requested before the update services were initialized");

            return;
        }

        _updateDialog.Show(_notchWindow, CreateUpdateViewModel).StartCheck();
    }

    private UpdateViewModel CreateUpdateViewModel()
    {

        var service = _updateCheckService ?? throw new InvalidOperationException("Update services are not initialized.");

        return new UpdateViewModel(new UpdateDialogDependencies
        {
            CheckAsync = cancellationToken => service.CheckAsync(UpdateCheckTrigger.Manual, cancellationToken),
            SaveSkippedVersionAsync = SaveSkippedVersionAsync,
            CurrentVersion = service.CurrentVersion,
            ApplyAsync = ApplyUpdateAsync,
            RequestShutdown = RequestUpdateShutdown,
            OpenUrl = OpenUrl,
        });
    }

    private async Task<bool> SaveSkippedVersionAsync(string version, CancellationToken cancellationToken)
    {

        var store = _updateSettingsStore ?? throw new InvalidOperationException("Update settings are not initialized.");
        var settings = await store.LoadAsync(cancellationToken);

        return await store.SaveAsync(settings with { SkippedVersion = version }, cancellationToken);
    }

    private static async Task<UpdateApplyResult> ApplyUpdateAsync(
        ReleaseInfo release,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {

        var appDirectory = AppContext.BaseDirectory;
        var executableName = Path.GetFileName(Environment.ProcessPath) ?? UpdateStartup.DEFAULT_EXECUTABLE_NAME;
        var mode = InstallModeDetector.DetectMode(appDirectory);

        using var downloader = new UpdateDownloader();
        var applier = new PortableUpdateApplier(new UpdateSwapJournal());
        var launcher = new InstallerUpdateLauncher();
        var coordinator = new UpdateApplyCoordinator(new UpdateApplySteps
        {
            Mode = mode,
            Architecture = RuntimeInformation.ProcessArchitecture,
            IsWritable = () => InstallModeDetector.IsWritable(appDirectory),
            DownloadAsync = downloader.DownloadAsync,
            ApplyPortable = zipPath => applier.Apply(zipPath, appDirectory, executableName),
            LaunchInstaller = launcher.Launch,
        });

        return await coordinator.ApplyAsync(release, progress, cancellationToken);
    }

    private void RequestUpdateShutdown()
    {

        Log.Information("Shutting down for the update");
        _ = ShutdownAsync();
    }

    private static void OpenUrl(Uri url)
    {

        if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("Refused to open non-HTTPS update link {Url}", url);

            return;
        }

        try
        {

            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {

            Log.Warning(ex, "Could not open update link {Url}", url);
        }
    }
}
