using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies the portable swap, rollback, and post-update cleanup on real files in a temporary folder (TC-14).
/// </summary>
public sealed class PortableUpdateApplierTests : IDisposable
{
    private const string EXE = "TokenHound.App.exe";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"portable_apply_test_{Guid.NewGuid():N}");
    private readonly string _appDirectory;
    private readonly string _updatesDirectory;
    private readonly List<ProcessStartInfo> _started = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PortableUpdateApplierTests"/> class with an installed "old" version.
    /// </summary>
    public PortableUpdateApplierTests()
    {

        _appDirectory = Path.Combine(_root, "app");
        _updatesDirectory = Path.Combine(_root, "updates");
        Directory.CreateDirectory(_appDirectory);
        Directory.CreateDirectory(_updatesDirectory);
        File.WriteAllText(Path.Combine(_appDirectory, EXE), "old exe");
        File.WriteAllText(Path.Combine(_appDirectory, "appsettings.json"), "old settings");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// Verifies that a successful apply swaps files, keeps the previous ones as .old, journals them, and starts the new exe with --updated.
    /// </summary>
    [Fact]
    public void Apply_Success_SwapsFilesAndStartsUpdatedExecutable()
    {

        var zip = CreateZip(("TokenHound.App.exe", "new exe"), ("appsettings.json", "new settings"), ("runtimes/win/native.dll", "native"));
        var journal = new UpdateSwapJournal(_updatesDirectory);

        CreateApplier(journal, started: true).Apply(zip, _appDirectory, EXE);

        Read(EXE).Should().Be("new exe");
        Read(EXE + UpdateSwapJournal.OLD_SUFFIX).Should().Be("old exe");
        Read("runtimes/win/native.dll").Should().Be("native");
        journal.Read()!.Files.Should().HaveCount(3);
        _started.Single().FileName.Should().Be(Path.Combine(_appDirectory, EXE));
        _started.Single().ArgumentList.Should().Equal(PortableUpdateApplier.UPDATED_ARGUMENT);
        Directory.Exists(Path.Combine(_updatesDirectory, "staging")).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a failed start restores every original file, removes added ones, and deletes the journal.
    /// </summary>
    [Fact]
    public void Apply_WhenStartFails_RestoresPreviousVersion()
    {

        var zip = CreateZip(("TokenHound.App.exe", "new exe"), ("appsettings.json", "new settings"), ("extra.dll", "added"));
        var journal = new UpdateSwapJournal(_updatesDirectory);

        var act = () => CreateApplier(journal, started: false).Apply(zip, _appDirectory, EXE);

        act.Should().Throw<UpdateApplyException>();
        Read(EXE).Should().Be("old exe");
        Read("appsettings.json").Should().Be("old settings");
        File.Exists(Path.Combine(_appDirectory, "extra.dll")).Should().BeFalse();
        Directory.GetFiles(_appDirectory, "*" + UpdateSwapJournal.OLD_SUFFIX).Should().BeEmpty();
        journal.Read().Should().BeNull();
    }

    /// <summary>
    /// Verifies that an archive without the executable is refused before any file changes.
    /// </summary>
    [Fact]
    public void Apply_WhenArchiveLacksExecutable_LeavesFilesUntouched()
    {

        var zip = CreateZip(("appsettings.json", "new settings"));
        var journal = new UpdateSwapJournal(_updatesDirectory);

        var act = () => CreateApplier(journal, started: true).Apply(zip, _appDirectory, EXE);

        act.Should().Throw<UpdateApplyException>();
        Read("appsettings.json").Should().Be("old settings");
        _started.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that cleanup deletes only the journaled .old files, removes the journal, and is idempotent.
    /// </summary>
    [Fact]
    public void CleanupAfterUpdate_DeletesJournaledOldFilesOnce()
    {

        var zip = CreateZip(("TokenHound.App.exe", "new exe"), ("appsettings.json", "new settings"));
        var journal = new UpdateSwapJournal(_updatesDirectory);
        CreateApplier(journal, started: true).Apply(zip, _appDirectory, EXE);
        File.WriteAllText(Path.Combine(_appDirectory, "user-notes.old"), "not ours");

        var first = journal.CleanupAfterUpdate();
        var second = journal.CleanupAfterUpdate();

        first.Should().Be(2);
        second.Should().Be(0);
        journal.Read().Should().BeNull();
        File.Exists(Path.Combine(_appDirectory, "user-notes.old")).Should().BeTrue();
    }

    private PortableUpdateApplier CreateApplier(UpdateSwapJournal journal, bool started)
        => new(journal, info =>
        {
            _started.Add(info);

            return started;
        });

    private string CreateZip(params (string Path, string Content)[] entries)
    {

        var zipPath = Path.Combine(_updatesDirectory, "TokenHound-1.4.0-win-x64-fxdependent.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var (entryPath, content) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry(entryPath).Open());
            writer.Write(content);
        }

        return zipPath;
    }

    private string Read(string relativePath)
        => File.ReadAllText(Path.Combine(_appDirectory, relativePath));
}
