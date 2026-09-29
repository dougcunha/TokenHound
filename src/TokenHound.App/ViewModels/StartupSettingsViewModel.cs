using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using TokenHound.Infrastructure.Startup;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presentation model for the "General" settings tab: turns starting TokenHound at Windows sign-in on and off.
/// The baseline is the operating system state, read when the dialog opens and again after each Apply.
/// </summary>
public sealed class StartupSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Error shown when the startup shortcut could not be read, created, or deleted.</summary>
    public const string APPLY_ERROR = "Failed to update the Windows startup shortcut.";

    private static readonly ILogger LOGGER = Log.ForContext<StartupSettingsViewModel>();

    private readonly IStartupLaunchService _service;
    private readonly string _executablePath;
    private readonly RelayCommand _applyCommand;

    private bool _isEnabled, _persistedEnabled, _isApplied;

    /// <summary>Initializes a new instance of the <see cref="StartupSettingsViewModel"/> class.</summary>
    /// <param name="service">The service managing the startup shortcut.</param>
    /// <param name="executablePath">The running executable the shortcut should start.</param>
    public StartupSettingsViewModel(IStartupLaunchService service, string executablePath)
    {

        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        _service = service;
        _executablePath = executablePath;
        _applyCommand = new RelayCommand(Apply, () => CanApply);
        _persistedEnabled = ReadState();
        _isEnabled = _persistedEnabled;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets a value indicating whether TokenHound should start at Windows sign-in.</summary>
    public bool IsEnabled
    {
        get
            => _isEnabled;
        set
        {

            if (_isEnabled == value)
                return;

            _isEnabled = value;
            ApplyError = null;
            _isApplied = false;
            Refresh();
        }
    }

    /// <summary>Gets the error of the most recent failed read or Apply, or <see langword="null"/>.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Gets a value indicating whether the most recent Apply succeeded and nothing was edited since.</summary>
    public bool IsApplied
        => _isApplied;

    /// <summary>Gets a value indicating whether the checkbox differs from the operating system state.</summary>
    public bool IsDirty
        => _isEnabled != _persistedEnabled;

    /// <summary>Gets a value indicating whether there is a change to apply.</summary>
    public bool CanApply
        => IsDirty;

    /// <summary>Gets the command executing <see cref="Apply"/>.</summary>
    public ICommand ApplyCommand
        => _applyCommand;

    /// <summary>Creates or deletes the startup shortcut, then reads the operating system state back as the new baseline.</summary>
    public void Apply()
    {

        if (!CanApply)
            return;

        var applied = _isEnabled
            ? TryRun(nameof(IStartupLaunchService.Enable), () => _service.Enable(_executablePath))
            : TryRun(nameof(IStartupLaunchService.Disable), _service.Disable);

        _persistedEnabled = ReadState();
        _isEnabled = _persistedEnabled;
        _isApplied = applied && ApplyError is null;
        Refresh();
    }

    private bool ReadState()
    {

        var enabled = false;

        TryRun(nameof(IStartupLaunchService.IsEnabled), () => enabled = _service.IsEnabled());

        return enabled;
    }

    private bool TryRun(string operationName, Action operation)
    {

        try
        {

            operation();

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException)
        {

            LOGGER.Warning(
                ex,
                "Startup shortcut {Operation} failed for {ExecutablePath}",
                operationName,
                _executablePath
            );
            ApplyError = APPLY_ERROR;

            return false;
        }
    }

    private void Refresh()
    {

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        _applyCommand.RaiseCanExecuteChanged();
    }

    private sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
            => execute();

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
