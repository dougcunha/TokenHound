using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Starts a verified Inno Setup installer silently, in the current user's context, asking it to relaunch TokenHound.
/// </summary>
public sealed class InstallerUpdateLauncher
{
    /// <summary>
    /// The installer arguments: no wizard, no message boxes, no reboot, relaunch the app after installing.
    /// </summary>
    public static readonly IReadOnlyList<string> SILENT_ARGUMENTS = ["/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/RELAUNCH=1"];

    private static readonly ILogger LOGGER = Log.ForContext<InstallerUpdateLauncher>();

    private readonly Func<ProcessStartInfo, bool> _startProcess;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallerUpdateLauncher"/> class.
    /// </summary>
    /// <param name="startProcess">Starts a process and reports whether it started; defaults to <see cref="Process.Start(ProcessStartInfo)"/>.</param>
    public InstallerUpdateLauncher(Func<ProcessStartInfo, bool>? startProcess = null)
    {

        _startProcess = startProcess ?? ProcessStarter.Start;
    }

    /// <summary>
    /// Builds the start information for <paramref name="setupPath"/>.
    /// </summary>
    /// <param name="setupPath">The verified setup executable.</param>
    /// <returns>Start information with an absolute path and the silent arguments.</returns>
    public static ProcessStartInfo CreateStartInfo(string setupPath)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(setupPath);

        var startInfo = new ProcessStartInfo(Path.GetFullPath(setupPath)) { UseShellExecute = false };

        foreach (var argument in SILENT_ARGUMENTS)
            startInfo.ArgumentList.Add(argument);

        return startInfo;
    }

    /// <summary>
    /// Starts the installer; the caller shuts the application down afterward so the installer can replace its files.
    /// </summary>
    /// <param name="setupPath">The verified setup executable.</param>
    /// <exception cref="UpdateApplyException">The setup file is missing or could not be started.</exception>
    public void Launch(string setupPath)
    {

        var startInfo = CreateStartInfo(setupPath);

        if (!File.Exists(startInfo.FileName))
            throw new UpdateApplyException($"The installer {startInfo.FileName} does not exist.");

        LOGGER.Information("UpdateApplyStarted {Mode}", "Installed");

        try
        {

            if (_startProcess(startInfo))
                return;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {

            throw new UpdateApplyException("The installer could not be started.", ex);
        }

        throw new UpdateApplyException("The installer could not be started.");
    }
}
