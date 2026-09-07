using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TokenHound.Infrastructure.System;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Finds a live Copilot-capable host process without reading or modifying process state.
/// </summary>
public sealed class CopilotProcessHostDetector
{
    private const string COPILOT_PROCESS_NAME = "copilot";
    private const string GH_PROCESS_NAME = "gh";
    private const string CODE_PROCESS_NAME = "Code";
    private const string COPILOT_EXTENSION_PATTERN = "github.copilot*";

    private readonly Func<IReadOnlyList<ProcessInfo>> _processEnumerator;
    private readonly Func<ProcessInfo, bool> _processLiveness;
    private readonly IReadOnlyList<string> _extensionDirectories;

    /// <summary>
    /// Initializes a detector using the current user's supported VS Code extension directories.
    /// </summary>
    /// <param name="userProfileDirectory">The user profile directory, or null for the current user.</param>
    /// <param name="processEnumerator">An optional process source for deterministic tests.</param>
    /// <param name="processLiveness">An optional liveness check for deterministic tests.</param>
    /// <param name="extensionDirectories">Optional supported extension roots.</param>
    public CopilotProcessHostDetector(
        string? userProfileDirectory = null,
        Func<IReadOnlyList<ProcessInfo>>? processEnumerator = null,
        Func<ProcessInfo, bool>? processLiveness = null,
        IReadOnlyList<string>? extensionDirectories = null)
    {

        var profileDirectory = string.IsNullOrWhiteSpace(userProfileDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : userProfileDirectory;
        _processEnumerator = processEnumerator ?? EnumerateProcesses;
        _processLiveness = processLiveness ?? IsProcessAlive;
        _extensionDirectories = extensionDirectories ??
        [
            Path.Combine(profileDirectory, ".vscode", "extensions"),
            Path.Combine(profileDirectory, ".vscode-insiders", "extensions")
        ];
    }

    /// <summary>
    /// Determines whether a live qualifying Copilot, gh, or supported Code host exists.
    /// </summary>
    /// <returns><see langword="true"/> when a qualifying live host exists.</returns>
    public bool HasQualifyingHost()
        => TryGetQualifyingHost(out _);

    /// <summary>
    /// Gets a live qualifying host process, if one exists.
    /// </summary>
    /// <param name="host">The qualifying host process information.</param>
    /// <returns><see langword="true"/> when a host was found.</returns>
    public bool TryGetQualifyingHost(out ProcessInfo? host)
    {

        host = null;
        var codeExtensionInstalled = HasCopilotExtension();

        foreach (var process in GetProcesses())
        {

            if (!IsSupportedProcess(process.Name, codeExtensionInstalled)
                || !_processLiveness(process))
                continue;

            host = process;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks supported VS Code extension roots for a GitHub Copilot extension directory.
    /// </summary>
    /// <returns><see langword="true"/> when a Copilot extension directory exists.</returns>
    public bool HasCopilotExtension()
    {

        foreach (var directory in _extensionDirectories)
        {

            if (HasCopilotExtensionIn(directory))
                return true;
        }

        return false;
    }

    private IReadOnlyList<ProcessInfo> GetProcesses()
    {

        try
        {
            return _processEnumerator() ?? [];
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or Win32Exception)
        {
            return [];
        }
    }

    private bool HasCopilotExtensionIn(string directory)
    {

        if (!Directory.Exists(directory))
            return false;

        try
        {
            foreach (var extension in Directory.EnumerateDirectories(
                directory,
                COPILOT_EXTENSION_PATTERN,
                SearchOption.TopDirectoryOnly))
            {

                if (Directory.Exists(extension))
                    return true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return false;
        }

        return false;
    }

    private static bool IsSupportedProcess(string name, bool codeExtensionInstalled)
        => IsProcessName(name, COPILOT_PROCESS_NAME)
            || IsProcessName(name, GH_PROCESS_NAME)
            || (codeExtensionInstalled && IsProcessName(name, CODE_PROCESS_NAME));

    private static bool IsProcessName(string actualName, string expectedName)
        => string.Equals(actualName, expectedName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(actualName, $"{expectedName}.exe", StringComparison.OrdinalIgnoreCase);

    private static bool IsProcessAlive(ProcessInfo process)
        => ProcessLiveness.IsProcessAlive(process.Pid, process.StartTimeUtc);

    private static IReadOnlyList<ProcessInfo> EnumerateProcesses()
    {

        var results = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {

            try
            {
                results.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Pid = process.Id,
                    StartTimeUtc = ProcessLiveness.GetProcessStartTimeUtc(process.Id)
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {

                // The process may exit or deny access while the snapshot is collected.
            }
            finally
            {

                process.Dispose();
            }
        }

        return results;
    }

    /// <summary>
    /// Describes the process data used for host qualification.
    /// </summary>
    public sealed record ProcessInfo
    {
        /// <summary>Gets the process name.</summary>
        public required string Name { get; init; }

        /// <summary>Gets the process identifier.</summary>
        public required int Pid { get; init; }

        /// <summary>Gets the process start time, when it was readable.</summary>
        public DateTimeOffset? StartTimeUtc { get; init; }
    }
}
