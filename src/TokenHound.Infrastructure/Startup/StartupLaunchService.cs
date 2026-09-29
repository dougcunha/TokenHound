using Serilog;
using System;
using System.IO;
using System.Runtime.Versioning;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Startup;

/// <summary>
/// Manages the per-user startup shortcut shared with the installer's <c>startupicon</c> task.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class StartupLaunchService : IStartupLaunchService
{
    /// <summary>
    /// The shortcut file name; it must stay equal to <c>{#MyAppName}.lnk</c> in <c>installer/TokenHound.iss</c>.
    /// </summary>
    public const string SHORTCUT_FILE_NAME = "TokenHound.lnk";

    private const string SHORTCUT_DESCRIPTION = "TokenHound";

    private static readonly ILogger LOGGER = Log.ForContext<StartupLaunchService>();

    private readonly string _startupFolder;
    private readonly IStartupApprovalStore _approvalStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupLaunchService"/> class.
    /// </summary>
    /// <param name="startupFolder">The Startup folder that holds the shortcut.</param>
    /// <param name="approvalStore">The store of Windows "Startup apps" approval values.</param>
    public StartupLaunchService(string startupFolder, IStartupApprovalStore approvalStore)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(startupFolder);
        ArgumentNullException.ThrowIfNull(approvalStore);

        _startupFolder = startupFolder;
        _approvalStore = approvalStore;
    }

    /// <summary>
    /// Gets the full path of the startup shortcut.
    /// </summary>
    public string ShortcutPath
        => Path.Combine(_startupFolder, SHORTCUT_FILE_NAME);

    /// <summary>
    /// Creates the service over the current user's Startup folder and the <c>HKEY_CURRENT_USER</c> approval key.
    /// The folder path is resolved even when the folder does not exist; <see cref="Enable"/> creates it.
    /// </summary>
    /// <returns>A service bound to the real per-user locations.</returns>
    public static StartupLaunchService CreateDefault()
        => new(ResolveStartupFolder(), new RegistryStartupApprovalStore());

    /// <summary>
    /// Resolves the current user's Startup folder path without requiring the folder to exist.
    /// </summary>
    /// <returns>The Startup folder path.</returns>
    public static string ResolveStartupFolder()
        => Environment.GetFolderPath(Environment.SpecialFolder.Startup, Environment.SpecialFolderOption.DoNotVerify);

    /// <inheritdoc />
    public bool IsEnabled()
        => File.Exists(ShortcutPath) && !IsDisabledByWindows();

    /// <inheritdoc />
    public void Enable(string executablePath)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        Directory.CreateDirectory(_startupFolder);

        var workingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;

        ShellLink.Save(
            ShortcutPath,
            executablePath,
            workingDirectory,
            SHORTCUT_DESCRIPTION
        );

        if (IsDisabledByWindows())
            _approvalStore.DeleteValue(SHORTCUT_FILE_NAME);

        LOGGER.Information("Startup shortcut {Operation} for {ExecutablePath}", nameof(Enable), executablePath);
    }

    /// <inheritdoc />
    public void Disable()
    {

        if (!File.Exists(ShortcutPath))
            return;

        File.Delete(ShortcutPath);
        LOGGER.Information("Startup shortcut {Operation} at {ShortcutPath}", nameof(Disable), ShortcutPath);
    }

    private bool IsDisabledByWindows()
        => StartupApprovalPolicy.IsDisabled(_approvalStore.ReadValue(SHORTCUT_FILE_NAME));
}
