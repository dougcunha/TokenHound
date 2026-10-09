using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Tracks the inputs of <see cref="HudBackdropPolicy"/> from Windows notifications on the HUD window, without polling.
/// </summary>
internal sealed class HudBackdropAvailability : IDisposable
{

    private const string PERSONALIZE_KEY = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string TRANSPARENCY_VALUE = "EnableTransparency";
    private const string IMMERSIVE_COLOR_SET = "ImmersiveColorSet";
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_POWERBROADCAST = 0x0218;
    private const int PBT_POWERSETTINGCHANGE = 0x8013;
    private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
    private const byte BATTERY_SAVER_ON = 1;
    private const int SETTING_DATA_OFFSET = 20;
    private static readonly Guid GUID_POWER_SAVING_STATUS = new("e00958c0-c213-4ace-ac77-fecced2eeea5");
    private static readonly Guid GUID_ENERGY_SAVER_STATUS = new("550e8400-e29b-41d4-a716-446655440000");
    private readonly Window _owner;
    private readonly HudBackdropPreference _preference;
    private HwndSource? _source;
    private IntPtr[] _registrations = [];
    private bool _powerSaving;
    private bool _energySaver;

    [StructLayout(LayoutKind.Sequential)]
    private struct PowerStatus
    {

        internal byte ACLineStatus;
        internal byte BatteryFlag;
        internal byte BatteryLifePercent;
        internal byte SystemStatusFlag;
        internal int BatteryLifeTime;
        internal int BatteryFullLifeTime;
    }

    internal HudBackdropAvailability(Window owner, HudBackdropPreference preference)
    {

        _owner = owner;
        _preference = preference;
        Inputs = Read();
        Publish(Inputs);
        preference.PropertyChanged += OnPreferenceChanged;
        SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
        owner.SourceInitialized += OnSourceInitialized;
    }

    /// <summary>Raised on the HUD thread when any input changed.</summary>
    internal event EventHandler? Changed;

    /// <summary>Gets the latest availability inputs.</summary>
    internal HudBackdropInputs Inputs { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {

        _preference.PropertyChanged -= OnPreferenceChanged;
        SystemParameters.StaticPropertyChanged -= OnSystemParameterChanged;
        _owner.SourceInitialized -= OnSourceInitialized;
        _source?.RemoveHook(WndProc);
        _source = null;

        foreach (var registration in _registrations)
            UnregisterPowerSettingNotification(registration);

        _registrations = [];
    }

    private void OnSourceInitialized(object? sender, EventArgs args)
    {

        var handle = new WindowInteropHelper(_owner).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        _registrations = [Register(handle, GUID_POWER_SAVING_STATUS), Register(handle, GUID_ENERGY_SAVER_STATUS)];
        Refresh();
    }

    private IntPtr WndProc(IntPtr handle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {

        if (message == WM_POWERBROADCAST && wParam.ToInt64() == PBT_POWERSETTINGCHANGE && lParam != IntPtr.Zero)
            ReadPowerSetting(lParam);

        if (IsColorSetChange(message, lParam) || (message == WM_POWERBROADCAST && wParam.ToInt64() == PBT_POWERSETTINGCHANGE))
            Refresh();

        return IntPtr.Zero;
    }

    private void OnPreferenceChanged(object? sender, PropertyChangedEventArgs args)
    {

        if (string.Equals(args.PropertyName, nameof(HudBackdropPreference.IsEnabled), StringComparison.OrdinalIgnoreCase))
            Refresh();
    }

    private void OnSystemParameterChanged(object? sender, PropertyChangedEventArgs args)
    {

        if (string.Equals(args.PropertyName, nameof(SystemParameters.HighContrast), StringComparison.OrdinalIgnoreCase))
            Refresh();
    }

    private void Refresh()
    {

        var next = Read();

        if (next == Inputs)
            return;

        Inputs = next;
        Publish(next);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shares the Windows input that blocks the material, ignoring the setting, for the Settings notice.</summary>
    private void Publish(HudBackdropInputs inputs)
    {

        var reason = HudBackdropPolicy.Reason(inputs with { IsEnabled = true });
        _preference.Unavailability = string.Equals(reason, HudBackdropPolicy.AVAILABLE, StringComparison.OrdinalIgnoreCase)
            ? null
            : reason;
    }

    private HudBackdropInputs Read()
        => new()
        {
            IsEnabled = _preference.IsEnabled,
            OsBuild = Environment.OSVersion.Version.Build,
            TransparencyEnabled = ReadTransparency(),
            EnergySaverActive = _powerSaving || _energySaver || (GetSystemPowerStatus(out var status) && status.SystemStatusFlag == BATTERY_SAVER_ON),
            HighContrast = SystemParameters.HighContrast
        };

    /// <summary>Reads a <c>POWERBROADCAST_SETTING</c>; Windows sends the current value on registration and on every change.</summary>
    private void ReadPowerSetting(IntPtr setting)
    {

        var guid = Marshal.PtrToStructure<Guid>(setting);
        var active = Marshal.ReadInt32(setting, SETTING_DATA_OFFSET) != 0;

        if (guid == GUID_POWER_SAVING_STATUS)
            _powerSaving = active;
        else if (guid == GUID_ENERGY_SAVER_STATUS)
            _energySaver = active;
    }

    private static bool IsColorSetChange(int message, IntPtr lParam)
        => message == WM_SETTINGCHANGE
           && lParam != IntPtr.Zero
           && string.Equals(Marshal.PtrToStringUni(lParam), IMMERSIVE_COLOR_SET, StringComparison.OrdinalIgnoreCase);

    private static bool ReadTransparency()
    {

        using var key = Registry.CurrentUser.OpenSubKey(PERSONALIZE_KEY);

        return key?.GetValue(TRANSPARENCY_VALUE) is not int value || value != 0;
    }

    private static IntPtr Register(IntPtr handle, Guid setting)
        => RegisterPowerSettingNotification(handle, in setting, DEVICE_NOTIFY_WINDOW_HANDLE);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out PowerStatus status);

    [DllImport("user32.dll")]
    private static extern IntPtr RegisterPowerSettingNotification(IntPtr recipient, in Guid setting, int flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterPowerSettingNotification(IntPtr handle);
}
