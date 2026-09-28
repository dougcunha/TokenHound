using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Updates;

namespace TokenHound.App.ViewModels;

/// <summary>
/// The delegates the update dialog runs; the App supplies them from the update services.
/// </summary>
public sealed record UpdateDialogDependencies
{
    /// <summary>Gets the delegate that runs (or joins) an update check.</summary>
    public required Func<CancellationToken, Task<UpdateCheckOutcome>> CheckAsync { get; init; }

    /// <summary>Gets the delegate that persists a skipped version and reports whether it was saved.</summary>
    public required Func<string, CancellationToken, Task<bool>> SaveSkippedVersionAsync { get; init; }

    /// <summary>Gets the running version, or <see langword="null"/> when it is not a release version.</summary>
    public ReleaseVersion? CurrentVersion { get; init; }

    /// <summary>Gets the step that downloads, verifies, and applies the offered release, or <see langword="null"/> when updating is unavailable.</summary>
    public Func<ReleaseInfo, IProgress<double>, CancellationToken, Task<UpdateApplyResult>>? ApplyAsync { get; init; }

    /// <summary>Gets the action that starts the graceful application shutdown after an update was handed off.</summary>
    public Action? RequestShutdown { get; init; }

    /// <summary>Gets the action that opens a web page, or <see langword="null"/>.</summary>
    public Action<Uri>? OpenUrl { get; init; }
}
