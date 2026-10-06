using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace TokenHound.App.Interop;

public sealed partial class DisplayCatalog
{
    private static IReadOnlyDictionary<string, TargetName> QueryTargets()
    {

        var targets = new Dictionary<string, TargetName>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in QueryActivePaths())
            AddTarget(targets, path);

        return targets;
    }

    private static DISPLAYCONFIG_PATH_INFO[] QueryActivePaths()
    {

        var status = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount);

        if (status == ERROR_SUCCESS)
            return FillActivePaths(pathCount, modeCount);

        LOGGER.Warning("GetDisplayConfigBufferSizes failed with {Status}; displays use GDI names", status);

        return [];
    }

    private static DISPLAYCONFIG_PATH_INFO[] FillActivePaths(uint pathCount, uint modeCount)
    {

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        var status = QueryDisplayConfig(
            QDC_ONLY_ACTIVE_PATHS,
            ref pathCount,
            paths,
            ref modeCount,
            modes,
            IntPtr.Zero
        );

        if (status == ERROR_SUCCESS)
            return paths[..(int)pathCount];

        LOGGER.Warning("QueryDisplayConfig failed with {Status}; displays use GDI names", status);

        return [];
    }

    private static void AddTarget(Dictionary<string, TargetName> targets, DISPLAYCONFIG_PATH_INFO path)
    {

        var gdiName = ReadSourceName(path.sourceInfo);
        var target = ReadTargetName(path.targetInfo);

        // A cloned source drives several targets; the first target names it.
        if (gdiName is not null && target is not null)
            targets.TryAdd(gdiName, target);
    }

    private static string? ReadSourceName(DISPLAYCONFIG_PATH_SOURCE_INFO source)
    {

        var request = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                adapterId = source.adapterId,
                id = source.id
            }
        };

        return DisplayConfigGetDeviceInfo(ref request) == ERROR_SUCCESS ? request.viewGdiDeviceName : null;
    }

    private static TargetName? ReadTargetName(DISPLAYCONFIG_PATH_TARGET_INFO target)
    {

        var request = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                adapterId = target.adapterId,
                id = target.id
            }
        };

        if (DisplayConfigGetDeviceInfo(ref request) != ERROR_SUCCESS)
            return null;

        var edidKey = (request.flags & EDID_IDS_VALID) != 0
            ? $"{request.edidManufactureId:X4}:{request.edidProductCodeId:X4}"
            : null;

        return new TargetName(request.monitorDevicePath, edidKey, request.monitorFriendlyDeviceName);
    }

    private static string ReadDpiAwareness()
        => GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()) switch
        {
            0 => "Unaware",
            1 => "SystemAware",
            2 => "PerMonitorAware",
            var other => other.ToString(CultureInfo.InvariantCulture)
        };

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll")]
    private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);

    private sealed record TargetName(string? DevicePath, string? EdidKey, string? FriendlyName);
}
