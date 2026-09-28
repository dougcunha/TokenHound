using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Applies a verified portable zip in place: extracts it, renames each existing file to <c>.old</c>, moves the new
/// file in, journals the swap, and starts the new executable with <c>--updated</c>. Any failure restores the previous files.
/// </summary>
public sealed class PortableUpdateApplier
{
    /// <summary>
    /// The argument that makes a relaunched executable wait for the previous instance and clean up.
    /// </summary>
    public const string UPDATED_ARGUMENT = "--updated";

    private const string STAGING_FOLDER_NAME = "staging";

    private static readonly ILogger LOGGER = Log.ForContext<PortableUpdateApplier>();

    private readonly UpdateSwapJournal _journal;
    private readonly Func<ProcessStartInfo, bool> _startProcess;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortableUpdateApplier"/> class.
    /// </summary>
    /// <param name="journal">The swap journal read by the relaunched executable.</param>
    /// <param name="startProcess">Starts a process and reports whether it started; defaults to <see cref="Process.Start(ProcessStartInfo)"/>.</param>
    /// <param name="timeProvider">An optional clock for the journal timestamp.</param>
    public PortableUpdateApplier(
        UpdateSwapJournal journal,
        Func<ProcessStartInfo, bool>? startProcess = null,
        TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(journal);

        _journal = journal;
        _startProcess = startProcess ?? ProcessStarter.Start;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Swaps the files of <paramref name="appDirectory"/> with the content of <paramref name="zipPath"/> and starts the new executable.
    /// </summary>
    /// <param name="zipPath">The verified portable zip.</param>
    /// <param name="appDirectory">The folder of the running executable.</param>
    /// <param name="executableName">The executable file name, for example <c>TokenHound.App.exe</c>.</param>
    /// <exception cref="UpdateApplyException">The archive is invalid, a file could not be swapped, or the new executable did not start; previous files were restored.</exception>
    public void Apply(string zipPath, string appDirectory, string executableName)
    {

        LOGGER.Information("UpdateApplyStarted {Mode}", "Portable");

        var stagingDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(zipPath))!, STAGING_FOLDER_NAME);
        var swapped = new List<SwappedFile>();

        try
        {

            var files = Extract(zipPath, stagingDirectory, executableName);
            Swap(new SwapPlan(stagingDirectory, appDirectory, files), swapped);
            StartUpdated(Path.Combine(appDirectory, executableName), appDirectory);
        }
        catch (Exception ex)
        {

            RollBack(swapped, ex);

            if (ex is UpdateApplyException)
                throw;

            throw new UpdateApplyException("The update could not be applied; the previous version was restored.", ex);
        }
        finally
        {

            TryDeleteDirectory(stagingDirectory);
        }
    }

    private static IReadOnlyList<string> Extract(string zipPath, string stagingDirectory, string executableName)
    {

        TryDeleteDirectory(stagingDirectory);
        ZipFile.ExtractToDirectory(zipPath, stagingDirectory);

        if (!File.Exists(Path.Combine(stagingDirectory, executableName)))
            throw new UpdateApplyException($"The update archive does not contain {executableName}.");

        return UpdateSwapJournal.ListRelativeFiles(stagingDirectory);
    }

    private void Swap(SwapPlan plan, List<SwappedFile> swapped)
    {

        _journal.Write(new SwapJournalEntry { AppDirectory = plan.AppDirectory, Files = plan.Files, CreatedUtc = _timeProvider.GetUtcNow() });

        foreach (var relativePath in plan.Files)
        {
            var target = Path.Combine(plan.AppDirectory, relativePath);
            var hadOriginal = File.Exists(target);

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            if (hadOriginal)
                File.Move(target, target + UpdateSwapJournal.OLD_SUFFIX, true);

            swapped.Add(new SwappedFile(target, hadOriginal));
            File.Move(Path.Combine(plan.StagingDirectory, relativePath), target);
        }
    }

    private void RollBack(List<SwappedFile> swapped, Exception cause)
    {

        Restore(swapped);
        _journal.Delete();
        LOGGER.Error(
            cause,
            "UpdateApplyFailed {Mode} {Error}",
            "Portable",
            cause.Message
        );
    }

    private void StartUpdated(string executablePath, string appDirectory)
    {

        var startInfo = new ProcessStartInfo(executablePath) { UseShellExecute = false, WorkingDirectory = appDirectory };
        startInfo.ArgumentList.Add(UPDATED_ARGUMENT);

        if (!_startProcess(startInfo))
            throw new UpdateApplyException("The updated executable did not start; the previous version was restored.");
    }

    private static void Restore(List<SwappedFile> swapped)
    {

        for (var index = swapped.Count - 1; index >= 0; index--)
        {
            var (target, hadOriginal) = swapped[index];
            var oldPath = target + UpdateSwapJournal.OLD_SUFFIX;

            if (hadOriginal && File.Exists(oldPath))
                File.Move(oldPath, target, true);
            else if (!hadOriginal && File.Exists(target))
                File.Delete(target);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {

            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            LOGGER.Warning(ex, "Could not delete update staging folder {Path}", path);
        }
    }

    private readonly record struct SwappedFile(string Target, bool HadOriginal);

    private sealed record SwapPlan(string StagingDirectory, string AppDirectory, IReadOnlyList<string> Files);
}
