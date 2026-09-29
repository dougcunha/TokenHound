using Microsoft.Win32;
using System;

namespace TokenHound.Infrastructure.Startup;

/// <summary>
/// Accesses the per-user <c>StartupApproved\StartupFolder</c> key that Explorer and Task Manager own; read and delete only.
/// </summary>
public sealed class RegistryStartupApprovalStore : IStartupApprovalStore
{
    /// <summary>
    /// The <c>HKEY_CURRENT_USER</c> subkey holding Startup-folder approval values.
    /// </summary>
    public const string KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    /// <inheritdoc />
    public byte[]? ReadValue(string name)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!OperatingSystem.IsWindows())
            return null;

        using var key = Registry.CurrentUser.OpenSubKey(KEY_PATH, writable: false);

        return key?.GetValue(name) as byte[];
    }

    /// <inheritdoc />
    public void DeleteValue(string name)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!OperatingSystem.IsWindows())
            return;

        using var key = Registry.CurrentUser.OpenSubKey(KEY_PATH, writable: true);

        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
