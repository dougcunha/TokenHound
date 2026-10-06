using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.Interop;

/// <summary>
/// Lists the connected displays with stable identity (monitor device path and EDID key), friendly name, primary flag,
/// and work area in physical pixels. Read-only and synchronous; call it from the UI thread.
/// </summary>
public sealed partial class DisplayCatalog
{
    private const string GDI_DEVICE_PREFIX = @"\\.\DISPLAY";

    private static readonly ILogger LOGGER = Log.ForContext<DisplayCatalog>();

    private string? _lastSignature;

    /// <summary>
    /// Enumerates the connected displays ordered by display number. When the display configuration query fails the
    /// displays still appear, identified by GDI device name and named <c>Display N</c>. Never throws.
    /// </summary>
    /// <returns>The connected displays; empty only when Windows reports no monitor at all.</returns>
    public IReadOnlyList<DisplayInfo> GetDisplays()
    {

        var targets = QueryTargets();
        var monitors = EnumerateMonitors();

        if (monitors.Count == 0)
            monitors = ReadPrimaryMonitor();

        var displays = monitors
            .Select((monitor, index) => ToDisplay(monitor, index, targets))
            .OrderBy(static display => display.Number)
            .ToList();

        LogIfChanged(displays);

        return displays;
    }

    private static List<MonitorEntry> EnumerateMonitors()
    {

        var entries = new List<MonitorEntry>();

        EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
            {

                if (TryReadMonitor(hMonitor, out var entry))
                    entries.Add(entry);

                return true;
            },
            IntPtr.Zero
        );

        return entries;
    }

    private static List<MonitorEntry> ReadPrimaryMonitor()
    {

        LOGGER.Warning("Display enumeration returned no monitor; falling back to the primary monitor");

        var primary = MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY);

        return TryReadMonitor(primary, out var entry) ? [entry] : [];
    }

    private static bool TryReadMonitor(IntPtr hMonitor, out MonitorEntry entry)
    {

        var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };

        if (hMonitor == IntPtr.Zero || !GetMonitorInfo(hMonitor, ref info))
        {

            entry = default;

            return false;
        }

        entry = new MonitorEntry(
            info.szDevice,
            info.rcWork,
            info.rcMonitor,
            (info.dwFlags & MONITORINFOF_PRIMARY) != 0
        );

        return true;
    }

    private static DisplayInfo ToDisplay(MonitorEntry monitor, int index, IReadOnlyDictionary<string, TargetName> targets)
    {

        var number = ParseNumber(monitor.Device) ?? index + 1;
        targets.TryGetValue(monitor.Device, out var target);
        var (width, height) = ReadResolution(monitor);

        return new()
        {
            DevicePath = string.IsNullOrWhiteSpace(target?.DevicePath) ? monitor.Device : target.DevicePath,
            EdidKey = target?.EdidKey,
            Name = string.IsNullOrWhiteSpace(target?.FriendlyName) ? $"Display {number}" : target.FriendlyName,
            Number = number,
            IsPrimary = monitor.IsPrimary,
            WorkArea = ToBounds(monitor.Work),
            Width = width,
            Height = height
        };
    }

    private static (int Width, int Height) ReadResolution(MonitorEntry monitor)
    {

        var mode = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };

        if (EnumDisplaySettings(monitor.Device, ENUM_CURRENT_SETTINGS, ref mode) && mode.dmPelsWidth > 0)
            return ((int)mode.dmPelsWidth, (int)mode.dmPelsHeight);

        return (monitor.Bounds.Right - monitor.Bounds.Left, monitor.Bounds.Bottom - monitor.Bounds.Top);
    }

    private static int? ParseNumber(string device)
    {

        if (!device.StartsWith(GDI_DEVICE_PREFIX, StringComparison.OrdinalIgnoreCase))
            return null;

        return int.TryParse(device.AsSpan(GDI_DEVICE_PREFIX.Length), out var number) ? number : null;
    }

    private static ScreenBounds ToBounds(RECT rect)
        => new()
        {
            Left = rect.Left,
            Top = rect.Top,
            Width = rect.Right - rect.Left,
            Height = rect.Bottom - rect.Top
        };

    private void LogIfChanged(IReadOnlyList<DisplayInfo> displays)
    {

        var signature = string.Join('|', displays.Select(static display => $"{display.DevicePath}:{display.IsPrimary}:{display.WorkArea}"));

        if (string.Equals(signature, _lastSignature, StringComparison.Ordinal))
            return;

        _lastSignature = signature;
        LOGGER.Debug("Display list changed: {Count} display(s), DPI awareness {Awareness}", displays.Count, ReadDpiAwareness());

        foreach (var display in displays)
        {

            LOGGER.Debug(
                "Display {Number} {Name} {Width}x{Height} primary={IsPrimary} work area {WorkArea} path {DevicePath}",
                display.Number,
                display.Name,
                display.Width,
                display.Height,
                display.IsPrimary,
                display.WorkArea,
                display.DevicePath
            );
        }
    }

    private readonly record struct MonitorEntry(
        string Device,
        RECT Work,
        RECT Bounds,
        bool IsPrimary
    );
}
